using Endpoint.Models;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.Data;

namespace Endpoint.Data.Initializers;

public class DatabaseInitializer
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly string _connectionString;

    public DatabaseInitializer(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<DatabaseInitializer> logger)
    {
        _context = context;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection") ??
            throw new InvalidOperationException("连接字符串 'DefaultConnection' 未在appsettings.json中找到。");
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("开始数据库初始化...");

            // 检查并创建表
            await CreateTablesAsync();

            // 检查是否需要填充种子数据
            try
            {
                if (!(await _context.Products.AnyAsync()) && !(await _context.Customers.AnyAsync()))
                {
                    _logger.LogInformation("开始填充种子数据...");
                    await SeedDataAsync();
                    _logger.LogInformation("种子数据已成功填充。");
                }
                else
                {
                    _logger.LogInformation("数据库已包含数据，跳过种子数据填充。");
                }
            }
            catch (MySqlException ex) when (ex.Message.Contains("doesn't exist"))
            {
                _logger.LogInformation("表不存在，将重新创建表结构...");
                await CreateTablesAsync(true);
                await SeedDataAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化数据库时发生错误。");
            throw;
        }
    }

    private async Task CreateTablesAsync(bool forceRecreate = false)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            if (forceRecreate)
            {
                // 删除现有表（如果存在）
                var dropTablesSql = @"
                    SET FOREIGN_KEY_CHECKS = 0;
                    DROP TABLE IF EXISTS OrderItems;
                    DROP TABLE IF EXISTS Orders;
                    DROP TABLE IF EXISTS Products;
                    DROP TABLE IF EXISTS Customers;
                    SET FOREIGN_KEY_CHECKS = 1;";

                using var dropCmd = new MySqlCommand(dropTablesSql, connection);
                await dropCmd.ExecuteNonQueryAsync();
            }

            // 创建表
            var createTablesSql = @"
                CREATE TABLE IF NOT EXISTS Products (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Name VARCHAR(100) NOT NULL,
                    Description TEXT,
                    Price DECIMAL(18, 2) NOT NULL,
                    StockQuantity INT NOT NULL,
                    CreatedAt DATETIME NOT NULL,
                    UpdatedAt DATETIME
                );

                CREATE TABLE IF NOT EXISTS Customers (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Name VARCHAR(100) NOT NULL,
                    Email VARCHAR(100) NOT NULL,
                    PhoneNumber VARCHAR(20),
                    Address TEXT,
                    CreatedAt DATETIME NOT NULL,
                    UpdatedAt DATETIME,
                    UNIQUE KEY UK_Email (Email)
                );

                CREATE TABLE IF NOT EXISTS Orders (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    CustomerId INT NOT NULL,
                    OrderDate DATETIME NOT NULL,
                    Status INT NOT NULL,
                    TotalAmount DECIMAL(18, 2) NOT NULL,
                    ShippingAddress TEXT,
                    ShippedDate DATETIME,
                    DeliveredDate DATETIME,
                    CreatedAt DATETIME NOT NULL,
                    UpdatedAt DATETIME,
                    FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
                );

                CREATE TABLE IF NOT EXISTS OrderItems (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    OrderId INT NOT NULL,
                    ProductId INT NOT NULL,
                    Quantity INT NOT NULL,
                    UnitPrice DECIMAL(18, 2) NOT NULL,
                    TotalPrice DECIMAL(18, 2) NOT NULL,
                    CreatedAt DATETIME NOT NULL,
                    UpdatedAt DATETIME,
                    FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE,
                    FOREIGN KEY (ProductId) REFERENCES Products(Id)
                );";

            using var createCmd = new MySqlCommand(createTablesSql, connection);
            await createCmd.ExecuteNonQueryAsync();
            _logger.LogInformation("数据库表已成功创建");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建数据库表时发生错误");
            throw;
        }
    }

    private async Task SeedDataAsync()
    {
        try
        {
            // 添加产品
            var products = new List<Product>
            {
                new Product
                {
                    Name = "笔记本电脑",
                    Description = "高性能笔记本电脑配备16GB内存和512GB固态硬盘",
                    Price = 6999.99m,
                    StockQuantity = 25
                },
                new Product
                {
                    Name = "智能手机",
                    Description = "最新型号，128GB存储和5G功能",
                    Price = 4999.99m,
                    StockQuantity = 50
                },
                new Product
                {
                    Name = "耳机",
                    Description = "降噪头戴式耳机",
                    Price = 999.99m,
                    StockQuantity = 100
                },
                new Product
                {
                    Name = "平板电脑",
                    Description = "10英寸平板电脑配备64GB存储",
                    Price = 2499.99m,
                    StockQuantity = 30
                },
                new Product
                {
                    Name = "智能手表",
                    Description = "健身追踪和通知功能",
                    Price = 1499.99m,
                    StockQuantity = 40
                }
            };
            await _context.Products.AddRangeAsync(products);
            await _context.SaveChangesAsync();
            _logger.LogInformation("已添加5个产品种子数据");

            // 添加客户
            var customers = new List<Customer>
            {
                new Customer
                {
                    Name = "张三",
                    Email = "zhangsan@example.com",
                    PhoneNumber = "138-1234-5678",
                    Address = "北京市海淀区123号"
                },
                new Customer
                {
                    Name = "李四",
                    Email = "lisi@example.com",
                    PhoneNumber = "139-8765-4321",
                    Address = "上海市浦东新区456号"
                },
                new Customer
                {
                    Name = "王五",
                    Email = "wangwu@example.com",
                    PhoneNumber = "137-5555-7777",
                    Address = "广州市天河区789号"
                }
            };
            await _context.Customers.AddRangeAsync(customers);
            await _context.SaveChangesAsync();
            _logger.LogInformation("已添加3个客户种子数据");

            // 添加订单
            var orders = new List<Order>
            {
                new Order
                {
                    CustomerId = customers[0].Id,
                    Status = OrderStatus.Delivered,
                    OrderDate = DateTime.UtcNow.AddDays(-10),
                    ShippedDate = DateTime.UtcNow.AddDays(-8),
                    DeliveredDate = DateTime.UtcNow.AddDays(-5),
                    ShippingAddress = customers[0].Address,
                    TotalAmount = 0 // 将根据订单项计算
                },
                new Order
                {
                    CustomerId = customers[1].Id,
                    Status = OrderStatus.Shipped,
                    OrderDate = DateTime.UtcNow.AddDays(-3),
                    ShippedDate = DateTime.UtcNow.AddDays(-1),
                    ShippingAddress = customers[1].Address,
                    TotalAmount = 0 // 将根据订单项计算
                },
                new Order
                {
                    CustomerId = customers[2].Id,
                    Status = OrderStatus.Processing,
                    OrderDate = DateTime.UtcNow.AddDays(-1),
                    ShippingAddress = customers[2].Address,
                    TotalAmount = 0 // 将根据订单项计算
                }
            };
            await _context.Orders.AddRangeAsync(orders);
            await _context.SaveChangesAsync();
            _logger.LogInformation("已添加3个订单种子数据");

            // 添加订单项
            var orderItems = new List<OrderItem>
            {
                new OrderItem
                {
                    OrderId = orders[0].Id,
                    ProductId = products[0].Id,
                    Quantity = 1,
                    UnitPrice = products[0].Price,
                    TotalPrice = products[0].Price * 1
                },
                new OrderItem
                {
                    OrderId = orders[0].Id,
                    ProductId = products[2].Id,
                    Quantity = 2,
                    UnitPrice = products[2].Price,
                    TotalPrice = products[2].Price * 2
                },
                new OrderItem
                {
                    OrderId = orders[1].Id,
                    ProductId = products[1].Id,
                    Quantity = 1,
                    UnitPrice = products[1].Price,
                    TotalPrice = products[1].Price * 1
                },
                new OrderItem
                {
                    OrderId = orders[1].Id,
                    ProductId = products[4].Id,
                    Quantity = 1,
                    UnitPrice = products[4].Price,
                    TotalPrice = products[4].Price * 1
                },
                new OrderItem
                {
                    OrderId = orders[2].Id,
                    ProductId = products[3].Id,
                    Quantity = 2,
                    UnitPrice = products[3].Price,
                    TotalPrice = products[3].Price * 2
                }
            };
            await _context.OrderItems.AddRangeAsync(orderItems);
            await _context.SaveChangesAsync();
            _logger.LogInformation("已添加5个订单项种子数据");

            // 更新订单总金额
            foreach (var order in orders)
            {
                order.TotalAmount = await _context.OrderItems
                    .Where(oi => oi.OrderId == order.Id)
                    .SumAsync(oi => oi.TotalPrice);
            }
            await _context.SaveChangesAsync();
            _logger.LogInformation("已更新所有订单的总金额");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "填充种子数据时发生错误");
            throw; // 重新抛出异常以便上层捕获
        }
    }
}