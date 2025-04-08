using Endpoint.Controllers;
using Endpoint.Models;
using Endpoint.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace MyApi.Tests.ControllerTests;

public class ProductsControllerTests
{
    private readonly Mock<IProductService> _mockProductService;
    private readonly Mock<ILogger<ProductsController>> _mockLogger;
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _mockProductService = new Mock<IProductService>();
        _mockLogger = new Mock<ILogger<ProductsController>>();
        _controller = new ProductsController(_mockProductService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetProducts_ReturnsOkResult_WithListOfProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Product 1", Price = 10.99m, StockQuantity = 100 },
            new() { Id = 2, Name = "Product 2", Price = 20.50m, StockQuantity = 50 }
        };

        _mockProductService.Setup(service => service.GetAllProductsAsync())
            .ReturnsAsync(products);

        // Act
        var result = await _controller.GetProducts();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedProducts = Assert.IsAssignableFrom<IEnumerable<Product>>(okResult.Value);
        Assert.Equal(2, returnedProducts.Count());
    }

    [Fact]
    public async Task GetProduct_WithExistingId_ReturnsOkResult_WithProduct()
    {
        // Arrange
        var product = new Product { Id = 1, Name = "Product 1", Price = 10.99m, StockQuantity = 100 };

        _mockProductService.Setup(service => service.GetProductByIdAsync(1))
            .ReturnsAsync(product);

        // Act
        var result = await _controller.GetProduct(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedProduct = Assert.IsType<Product>(okResult.Value);
        Assert.Equal(1, returnedProduct.Id);
        Assert.Equal("Product 1", returnedProduct.Name);
    }

    [Fact]
    public async Task GetProduct_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        _mockProductService.Setup(service => service.GetProductByIdAsync(999))
            .ReturnsAsync((Product)null);

        // Act
        var result = await _controller.GetProduct(999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateProduct_WithValidModel_ReturnsCreatedAtActionResult()
    {
        // Arrange
        var newProduct = new Product { Name = "New Product", Price = 15.99m, StockQuantity = 75 };
        var createdProduct = new Product { Id = 1, Name = "New Product", Price = 15.99m, StockQuantity = 75 };

        _mockProductService.Setup(service => service.CreateProductAsync(It.IsAny<Product>()))
            .ReturnsAsync(createdProduct);

        // Act
        var result = await _controller.CreateProduct(newProduct);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(ProductsController.GetProduct), createdAtActionResult.ActionName);
        Assert.Equal(1, createdAtActionResult.RouteValues["id"]);

        var returnedProduct = Assert.IsType<Product>(createdAtActionResult.Value);
        Assert.Equal(1, returnedProduct.Id);
        Assert.Equal("New Product", returnedProduct.Name);
    }

    [Fact]
    public async Task UpdateProduct_WithValidIdAndModel_ReturnsOkResult()
    {
        // Arrange
        var productId = 1;
        var updatedProduct = new Product
        {
            Id = productId,
            Name = "Updated Product",
            Price = 25.99m,
            StockQuantity = 150
        };

        _mockProductService.Setup(service => service.UpdateProductAsync(productId, It.IsAny<Product>()))
            .ReturnsAsync(updatedProduct);

        // Act
        var result = await _controller.UpdateProduct(productId, updatedProduct);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedProduct = Assert.IsType<Product>(okResult.Value);
        Assert.Equal(productId, returnedProduct.Id);
        Assert.Equal("Updated Product", returnedProduct.Name);
    }

    [Fact]
    public async Task DeleteProduct_WithExistingId_ReturnsNoContent()
    {
        // Arrange
        var productId = 1;

        _mockProductService.Setup(service => service.DeleteProductAsync(productId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteProduct(productId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteProduct_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var productId = 999;

        _mockProductService.Setup(service => service.DeleteProductAsync(productId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteProduct(productId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}