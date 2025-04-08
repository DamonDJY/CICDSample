using Endpoint.Models;
using Endpoint.Services;
using MyApi.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace MyApi.Tests.UnitTests;

public class OrderServiceTests : IDisposable
{
    private readonly Endpoint.Data.ApplicationDbContext _dbContext;
    private readonly OrderService _orderService;
    private int _testCustomerId;
    private int _testProductId;

    public OrderServiceTests()
    {
        _dbContext = TestDatabaseFactory.CreateTestDatabase();
        _orderService = new OrderService(_dbContext);
        SeedDatabase();
    }

    private void SeedDatabase()
    {
        // Add test customer
        var customer = new Customer
        {
            Name = "Test Customer",
            Email = "test@example.com",
            PhoneNumber = "1234567890",
            Address = "Test Address"
        };
        _dbContext.Customers.Add(customer);
        _dbContext.SaveChanges();
        _testCustomerId = customer.Id;

        // Add test product
        var product = new Product
        {
            Name = "Test Product",
            Price = 10.99m,
            StockQuantity = 100
        };
        _dbContext.Products.Add(product);
        _dbContext.SaveChanges();
        _testProductId = product.Id;

        // Add test orders
        _dbContext.Orders.AddRange(
            new Order
            {
                CustomerId = _testCustomerId,
                Status = OrderStatus.Pending,
                TotalAmount = 10.99m,
                ShippingAddress = "Test Address",
                OrderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        ProductId = _testProductId,
                        Quantity = 1,
                        UnitPrice = 10.99m,
                        TotalPrice = 10.99m
                    }
                }
            },
            new Order
            {
                CustomerId = _testCustomerId,
                Status = OrderStatus.Processing,
                TotalAmount = 21.98m,
                ShippingAddress = "Test Address",
                OrderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        ProductId = _testProductId,
                        Quantity = 2,
                        UnitPrice = 10.99m,
                        TotalPrice = 21.98m
                    }
                }
            }
        );
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetAllOrdersAsync_ShouldReturnAllOrders()
    {
        // Act
        var result = await _orderService.GetAllOrdersAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetOrderByIdAsync_WithExistingId_ShouldReturnOrder()
    {
        // Arrange
        var existingOrderId = _dbContext.Orders.First().Id;

        // Act
        var result = await _orderService.GetOrderByIdAsync(existingOrderId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingOrderId, result.Id);
        Assert.NotNull(result.Customer);
        Assert.NotEmpty(result.OrderItems);
    }

    [Fact]
    public async Task GetOrderByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Act
        var result = await _orderService.GetOrderByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetOrdersByCustomerIdAsync_ShouldReturnCustomerOrders()
    {
        // Act
        var result = await _orderService.GetOrdersByCustomerIdAsync(_testCustomerId);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, order => Assert.Equal(_testCustomerId, order.CustomerId));
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldAddNewOrder()
    {
        // Arrange
        var newOrder = new Order
        {
            CustomerId = _testCustomerId,
            Status = OrderStatus.Pending,
            ShippingAddress = "New Test Address",
            OrderItems = new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = _testProductId,
                    Quantity = 3,
                    UnitPrice = 10.99m,
                    TotalPrice = 32.97m
                }
            }
        };

        // Act
        var result = await _orderService.CreateOrderAsync(newOrder);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(0, result.Id);
        Assert.Equal(_testCustomerId, result.CustomerId);
        Assert.Equal(32.97m, result.TotalAmount);
        Assert.Equal(3, _dbContext.Orders.Count());
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_WithExistingId_ShouldUpdateStatus()
    {
        // Arrange
        var existingOrder = _dbContext.Orders.First();
        var newStatus = OrderStatus.Shipped;

        // Act
        var result = await _orderService.UpdateOrderStatusAsync(existingOrder.Id, newStatus);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newStatus, result.Status);
        Assert.NotNull(result.ShippedDate);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Act
        var result = await _orderService.UpdateOrderStatusAsync(999, OrderStatus.Shipped);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteOrderAsync_WithExistingId_ShouldRemoveOrder()
    {
        // Arrange
        var existingOrderId = _dbContext.Orders.First().Id;
        var initialCount = _dbContext.Orders.Count();

        // Act
        var result = await _orderService.DeleteOrderAsync(existingOrderId);

        // Assert
        Assert.True(result);
        Assert.Equal(initialCount - 1, _dbContext.Orders.Count());
    }

    [Fact]
    public async Task DeleteOrderAsync_WithNonExistingId_ShouldReturnFalse()
    {
        // Act
        var result = await _orderService.DeleteOrderAsync(999);

        // Assert
        Assert.False(result);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}