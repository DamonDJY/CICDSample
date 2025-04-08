using Endpoint.Models;
using Endpoint.Services;
using MyApi.Tests.Helpers;
using System.Linq;

namespace MyApi.Tests.UnitTests;

public class ProductServiceTests : IDisposable
{
    private readonly Endpoint.Data.ApplicationDbContext _dbContext;
    private readonly ProductService _productService;

    public ProductServiceTests()
    {
        _dbContext = TestDatabaseFactory.CreateTestDatabase();
        _productService = new ProductService(_dbContext);
        SeedDatabase();
    }

    private void SeedDatabase()
    {
        // Add test products
        _dbContext.Products.AddRange(
            new Product { Name = "Test Product 1", Price = 10.99m, StockQuantity = 100 },
            new Product { Name = "Test Product 2", Price = 20.50m, StockQuantity = 50 },
            new Product { Name = "Test Product 3", Price = 5.75m, StockQuantity = 200 }
        );
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetAllProductsAsync_ShouldReturnAllProducts()
    {
        // Act
        var result = await _productService.GetAllProductsAsync();

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetProductByIdAsync_WithExistingId_ShouldReturnProduct()
    {
        // Arrange
        var existingProductId = _dbContext.Products.First().Id;

        // Act
        var result = await _productService.GetProductByIdAsync(existingProductId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingProductId, result.Id);
    }

    [Fact]
    public async Task GetProductByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Act
        var result = await _productService.GetProductByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateProductAsync_ShouldAddNewProduct()
    {
        // Arrange
        var newProduct = new Product
        {
            Name = "New Test Product",
            Description = "Test Description",
            Price = 15.99m,
            StockQuantity = 75
        };

        // Act
        var result = await _productService.CreateProductAsync(newProduct);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(0, result.Id);
        Assert.Equal(newProduct.Name, result.Name);
        Assert.Equal(4, _dbContext.Products.Count());
    }

    [Fact]
    public async Task UpdateProductAsync_WithExistingId_ShouldUpdateProduct()
    {
        // Arrange
        var existingProduct = _dbContext.Products.First();
        var updatedProductData = new Product
        {
            Id = existingProduct.Id,
            Name = "Updated Product Name",
            Description = "Updated Description",
            Price = 25.99m,
            StockQuantity = 150
        };

        // Act
        var result = await _productService.UpdateProductAsync(existingProduct.Id, updatedProductData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updatedProductData.Name, result.Name);
        Assert.Equal(updatedProductData.Price, result.Price);
        Assert.Equal(updatedProductData.StockQuantity, result.StockQuantity);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task DeleteProductAsync_WithExistingId_ShouldRemoveProduct()
    {
        // Arrange
        var existingProductId = _dbContext.Products.First().Id;
        var initialCount = _dbContext.Products.Count();

        // Act
        var result = await _productService.DeleteProductAsync(existingProductId);

        // Assert
        Assert.True(result);
        Assert.Equal(initialCount - 1, _dbContext.Products.Count());
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}