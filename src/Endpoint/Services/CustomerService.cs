using Endpoint.Data;
using Endpoint.Models;
using Microsoft.EntityFrameworkCore;

namespace Endpoint.Services;

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _dbContext;

    public CustomerService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
    {
        return await _dbContext.Customers.ToListAsync();
    }

    public async Task<Customer?> GetCustomerByIdAsync(int id)
    {
        return await _dbContext.Customers.FindAsync(id);
    }

    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();
        return customer;
    }

    public async Task<Customer?> UpdateCustomerAsync(int id, Customer customer)
    {
        var existingCustomer = await _dbContext.Customers.FindAsync(id);
        if (existingCustomer == null)
        {
            return null;
        }

        existingCustomer.Name = customer.Name;
        existingCustomer.Email = customer.Email;
        existingCustomer.PhoneNumber = customer.PhoneNumber;
        existingCustomer.Address = customer.Address;
        existingCustomer.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return existingCustomer;
    }

    public async Task<bool> DeleteCustomerAsync(int id)
    {
        var customer = await _dbContext.Customers.FindAsync(id);
        if (customer == null)
        {
            return false;
        }

        _dbContext.Customers.Remove(customer);
        await _dbContext.SaveChangesAsync();
        return true;
    }
}