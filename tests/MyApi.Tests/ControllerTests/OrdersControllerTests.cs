using Endpoint.Controllers;
using Endpoint.Models;
using Endpoint.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace MyApi.Tests.ControllerTests;

public class OrdersControllerTests
{
    private readonly Mock<IOrderService> _mockOrderService;
    private readonly Mock<IProductService> _mockProductService;
    private readonly Mock<ILogger<OrdersController>> _mockLogger;
    private readonly OrdersController _controller;

    public OrdersControllerTests()
    {
        _mockOrderService = new Mock<IOrderService>();
        _mockProductService = new Mock<IProductService>();
        _mockLogger = new Mock<ILogger<OrdersController>>();
        _controller = new OrdersController(_mockOrderService.Object, _mockProductService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetOrders_ReturnsOkResult_WithListOfOrders()
    {
        // Arrange
        var orders = new List<Order>
        {
            new() { Id = 1, CustomerId = 1, TotalAmount = 10.99m, Status = OrderStatus.Pending },
            new() { Id = 2, CustomerId = 1, TotalAmount = 20.50m, Status = OrderStatus.Processing }
        };

        _mockOrderService.Setup(service => service.GetAllOrdersAsync())
            .ReturnsAsync(orders);

        // Act
        var result = await _controller.GetOrders();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedOrders = Assert.IsAssignableFrom<IEnumerable<Order>>(okResult.Value);
        Assert.Equal(2, returnedOrders.Count());
    }

    [Fact]
    public async Task GetOrder_WithExistingId_ReturnsOkResult_WithOrder()
    {
        // Arrange
        var order = new Order { Id = 1, CustomerId = 1, TotalAmount = 10.99m, Status = OrderStatus.Pending };

        _mockOrderService.Setup(service => service.GetOrderByIdAsync(1))
            .ReturnsAsync(order);

        // Act
        var result = await _controller.GetOrder(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedOrder = Assert.IsType<Order>(okResult.Value);
        Assert.Equal(1, returnedOrder.Id);
        Assert.Equal(OrderStatus.Pending, returnedOrder.Status);
    }

    [Fact]
    public async Task GetOrder_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        _mockOrderService.Setup(service => service.GetOrderByIdAsync(999))
            .ReturnsAsync((Order)null);

        // Act
        var result = await _controller.GetOrder(999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateOrder_WithValidModel_ReturnsCreatedAtActionResult()
    {
        // Arrange
        var product = new Product { Id = 1, Name = "Test Product", Price = 10.99m, StockQuantity = 100 };

        var orderItem = new OrderItem
        {
            ProductId = 1,
            Quantity = 2,
            UnitPrice = 10.99m,
            TotalPrice = 21.98m
        };

        var newOrder = new Order
        {
            CustomerId = 1,
            Status = OrderStatus.Pending,
            OrderItems = new List<OrderItem> { orderItem }
        };

        var createdOrder = new Order
        {
            Id = 1,
            CustomerId = 1,
            Status = OrderStatus.Pending,
            TotalAmount = 21.98m,
            OrderItems = new List<OrderItem> { orderItem }
        };

        _mockProductService.Setup(service => service.GetProductByIdAsync(1))
            .ReturnsAsync(product);

        _mockProductService.Setup(service => service.UpdateProductAsync(1, It.IsAny<Product>()))
            .ReturnsAsync(product);

        _mockOrderService.Setup(service => service.CreateOrderAsync(It.IsAny<Order>()))
            .ReturnsAsync(createdOrder);

        // Act
        var result = await _controller.CreateOrder(newOrder);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(OrdersController.GetOrder), createdAtActionResult.ActionName);
        Assert.Equal(1, createdAtActionResult.RouteValues["id"]);

        var returnedOrder = Assert.IsType<Order>(createdAtActionResult.Value);
        Assert.Equal(1, returnedOrder.Id);
        Assert.Equal(21.98m, returnedOrder.TotalAmount);
    }

    [Fact]
    public async Task CreateOrder_WithInvalidProduct_ReturnsBadRequest()
    {
        // Arrange
        var orderItem = new OrderItem
        {
            ProductId = 999, // Non-existing product
            Quantity = 2
        };

        var newOrder = new Order
        {
            CustomerId = 1,
            Status = OrderStatus.Pending,
            OrderItems = new List<OrderItem> { orderItem }
        };

        _mockProductService.Setup(service => service.GetProductByIdAsync(999))
            .ReturnsAsync((Product)null);

        // Act
        var result = await _controller.CreateOrder(newOrder);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Product with ID 999 not found", badRequestResult.Value.ToString());
    }

    [Fact]
    public async Task CreateOrder_WithInsufficientStock_ReturnsBadRequest()
    {
        // Arrange
        var product = new Product { Id = 1, Name = "Test Product", Price = 10.99m, StockQuantity = 1 };

        var orderItem = new OrderItem
        {
            ProductId = 1,
            Quantity = 2 // More than available stock
        };

        var newOrder = new Order
        {
            CustomerId = 1,
            Status = OrderStatus.Pending,
            OrderItems = new List<OrderItem> { orderItem }
        };

        _mockProductService.Setup(service => service.GetProductByIdAsync(1))
            .ReturnsAsync(product);

        // Act
        var result = await _controller.CreateOrder(newOrder);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Not enough stock for product Test Product", badRequestResult.Value.ToString());
    }

    [Fact]
    public async Task UpdateOrderStatus_WithExistingId_ReturnsOkResult()
    {
        // Arrange
        var orderId = 1;
        var newStatus = OrderStatus.Shipped;
        var updatedOrder = new Order
        {
            Id = orderId,
            CustomerId = 1,
            Status = newStatus,
            TotalAmount = 10.99m,
            ShippedDate = DateTime.UtcNow
        };

        _mockOrderService.Setup(service => service.UpdateOrderStatusAsync(orderId, newStatus))
            .ReturnsAsync(updatedOrder);

        // Act
        var result = await _controller.UpdateOrderStatus(orderId, newStatus);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedOrder = Assert.IsType<Order>(okResult.Value);
        Assert.Equal(orderId, returnedOrder.Id);
        Assert.Equal(newStatus, returnedOrder.Status);
        Assert.NotNull(returnedOrder.ShippedDate);
    }

    [Fact]
    public async Task UpdateOrderStatus_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var orderId = 999;
        var newStatus = OrderStatus.Shipped;

        _mockOrderService.Setup(service => service.UpdateOrderStatusAsync(orderId, newStatus))
            .ReturnsAsync((Order)null);

        // Act
        var result = await _controller.UpdateOrderStatus(orderId, newStatus);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteOrder_WithExistingId_ReturnsNoContent()
    {
        // Arrange
        var orderId = 1;

        _mockOrderService.Setup(service => service.DeleteOrderAsync(orderId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteOrder(orderId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteOrder_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var orderId = 999;

        _mockOrderService.Setup(service => service.DeleteOrderAsync(orderId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteOrder(orderId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}