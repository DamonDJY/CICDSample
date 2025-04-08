using Endpoint.Data;
using Endpoint.Models;
using Microsoft.EntityFrameworkCore;

namespace Endpoint.Services;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _dbContext;

    public OrderService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Order>> GetAllOrdersAsync()
    {
        return await _dbContext.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .ToListAsync();
    }

    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        return await _dbContext.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(int customerId)
    {
        return await _dbContext.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Where(o => o.CustomerId == customerId)
            .ToListAsync();
    }

    public async Task<Order> CreateOrderAsync(Order order)
    {
        // Calculate the total amount based on order items
        order.TotalAmount = order.OrderItems.Sum(oi => oi.TotalPrice);

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();
        return order;
    }

    public async Task<Order?> UpdateOrderStatusAsync(int id, OrderStatus status)
    {
        var existingOrder = await _dbContext.Orders.FindAsync(id);
        if (existingOrder == null)
        {
            return null;
        }

        existingOrder.Status = status;
        existingOrder.UpdatedAt = DateTime.UtcNow;

        // Update shipping and delivery dates based on status
        if (status == OrderStatus.Shipped && existingOrder.ShippedDate == null)
        {
            existingOrder.ShippedDate = DateTime.UtcNow;
        }
        else if (status == OrderStatus.Delivered && existingOrder.DeliveredDate == null)
        {
            existingOrder.DeliveredDate = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();
        return existingOrder;
    }

    public async Task<bool> DeleteOrderAsync(int id)
    {
        var order = await _dbContext.Orders.FindAsync(id);
        if (order == null)
        {
            return false;
        }

        _dbContext.Orders.Remove(order);
        await _dbContext.SaveChangesAsync();
        return true;
    }
}