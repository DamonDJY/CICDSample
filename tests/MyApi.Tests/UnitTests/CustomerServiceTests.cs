using System;
using Endpoint.Models;
using Endpoint.Services;
using Moq;
using MyApi.Tests.Helpers;
using System.Linq;
using Xunit;

namespace MyApi.Tests.UnitTests;

public class CustomerServiceTests : IDisposable
{
    private readonly Endpoint.Data.ApplicationDbContext _dbContext;
    private readonly ICustomerService _customerService;

    public CustomerServiceTests()
    {
        _dbContext = TestDatabaseFactory.CreateTestDatabase();
        _customerService = new CustomerService(_dbContext);
        SeedDatabase();
    }

    private void SeedDatabase()
    {
        // Add test customers
        _dbContext.Customers.AddRange(
            new Customer
            {
                Name = "Test Customer 1",
                Email = "customer1@example.com",
                PhoneNumber = "1234567890",
                Address = "Test Address 1"
            },
            new Customer
            {
                Name = "Test Customer 2",
                Email = "customer2@example.com",
                PhoneNumber = "0987654321",
                Address = "Test Address 2"
            },
            new Customer
            {
                Name = "Test Customer 3",
                Email = "customer3@example.com",
                PhoneNumber = "5555555555",
                Address = "Test Address 3"
            }
        );
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetAllCustomersAsync_ShouldReturnAllCustomers()
    {
        // Act
        var result = await _customerService.GetAllCustomersAsync();

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetCustomerByIdAsync_WithExistingId_ShouldReturnCustomer()
    {
        // Arrange
        var existingCustomerId = _dbContext.Customers.First().Id;

        // Act
        var result = await _customerService.GetCustomerByIdAsync(existingCustomerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingCustomerId, result.Id);
    }

    [Fact]
    public async Task GetCustomerByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Act
        var result = await _customerService.GetCustomerByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateCustomerAsync_ShouldAddNewCustomer()
    {
        // Arrange
        var newCustomer = new Customer
        {
            Name = "New Test Customer",
            Email = "newcustomer@example.com",
            PhoneNumber = "9999999999",
            Address = "New Test Address"
        };

        // Act
        var result = await _customerService.CreateCustomerAsync(newCustomer);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(0, result.Id);
        Assert.Equal(newCustomer.Name, result.Name);
        Assert.Equal(newCustomer.Email, result.Email);
        Assert.Equal(4, _dbContext.Customers.Count());
    }

    [Fact]
    public async Task UpdateCustomerAsync_WithExistingId_ShouldUpdateCustomer()
    {
        // Arrange
        var existingCustomer = _dbContext.Customers.First();
        var updatedCustomerData = new Customer
        {
            Id = existingCustomer.Id,
            Name = "Updated Customer Name",
            Email = "updated@example.com",
            PhoneNumber = "1112223333",
            Address = "Updated Address"
        };

        // Act
        var result = await _customerService.UpdateCustomerAsync(existingCustomer.Id, updatedCustomerData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updatedCustomerData.Name, result.Name);
        Assert.Equal(updatedCustomerData.Email, result.Email);
        Assert.Equal(updatedCustomerData.PhoneNumber, result.PhoneNumber);
        Assert.Equal(updatedCustomerData.Address, result.Address);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task UpdateCustomerAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        var updatedCustomerData = new Customer
        {
            Id = 999,
            Name = "Non-existent Customer",
            Email = "nonexistent@example.com",
            PhoneNumber = "0000000000",
            Address = "Non-existent Address"
        };

        // Act
        var result = await _customerService.UpdateCustomerAsync(999, updatedCustomerData);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteCustomerAsync_WithExistingId_ShouldRemoveCustomer()
    {
        // Arrange
        var existingCustomerId = _dbContext.Customers.First().Id;
        var initialCount = _dbContext.Customers.Count();

        // Act
        var result = await _customerService.DeleteCustomerAsync(existingCustomerId);

        // Assert
        Assert.True(result);
        Assert.Equal(initialCount - 1, _dbContext.Customers.Count());
    }

    [Fact]
    public async Task DeleteCustomerAsync_WithNonExistingId_ShouldReturnFalse()
    {
        // Act
        var result = await _customerService.DeleteCustomerAsync(999);

        // Assert
        Assert.False(result);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
