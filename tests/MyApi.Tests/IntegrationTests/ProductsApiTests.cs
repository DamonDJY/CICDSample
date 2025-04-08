// using Endpoint.Data;
// using Endpoint.Models;
// using Endpoint;  // 正确引用Endpoint项目
// using Microsoft.AspNetCore.Hosting;
// using Microsoft.AspNetCore.Mvc.Testing;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.DependencyInjection;
// using System.Net;
// using System.Net.Http.Json;
// using System.Text;
// using System.Text.Json;

// namespace MyApi.Tests.IntegrationTests;

// public class ProductsApiTests : IClassFixture<WebApplicationFactory<Program>>
// {
//     private readonly WebApplicationFactory<Program> _factory;
//     private readonly HttpClient _client;
//     private readonly JsonSerializerOptions _jsonOptions = new()
//     {
//         PropertyNamingPolicy = JsonNamingPolicy.CamelCase
//     };

//     public ProductsApiTests(WebApplicationFactory<Program> factory)
//     {
//         _factory = factory.WithWebHostBuilder(builder =>
//         {
//             builder.UseEnvironment("Testing");

//             builder.ConfigureServices(services =>
//             {
//                 // Remove the app's ApplicationDbContext registration
//                 var descriptor = services.SingleOrDefault(
//                     d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

//                 if (descriptor != null)
//                 {
//                     services.Remove(descriptor);
//                 }

//                 // Add ApplicationDbContext using an in-memory database for testing
//                 services.AddDbContext<ApplicationDbContext>(options =>
//                 {
//                     options.UseSqlite("DataSource=:memory:");
//                 });

//                 // Build the service provider
//                 var sp = services.BuildServiceProvider();

//                 // Create a scope to obtain a reference to the database context
//                 using var scope = sp.CreateScope();
//                 var scopedServices = scope.ServiceProvider;
//                 var db = scopedServices.GetRequiredService<ApplicationDbContext>();

//                 // Ensure the database is created and seed with test data
//                 db.Database.OpenConnection();
//                 db.Database.EnsureCreated();

//                 try
//                 {
//                     SeedDatabase(db);
//                 }
//                 catch
//                 {
//                     // Error seeding the database
//                 }
//             });
//         });

//         _client = _factory.CreateClient();
//     }

//     private void SeedDatabase(ApplicationDbContext context)
//     {
//         // Add test products
//         context.Products.AddRange(
//             new Product { Name = "Test Product 1", Price = 10.99m, StockQuantity = 100 },
//             new Product { Name = "Test Product 2", Price = 20.50m, StockQuantity = 50 },
//             new Product { Name = "Test Product 3", Price = 5.75m, StockQuantity = 200 }
//         );
//         context.SaveChanges();
//     }

//     [Fact]
//     public async Task GetAllProducts_ReturnsSuccessAndCorrectContentType()
//     {
//         // Act
//         var response = await _client.GetAsync("/api/products");

//         // Assert
//         response.EnsureSuccessStatusCode();
//         Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
//     }

//     [Fact]
//     public async Task GetAllProducts_ReturnsExpectedProducts()
//     {
//         // Act
//         var response = await _client.GetAsync("/api/products");

//         // Assert
//         response.EnsureSuccessStatusCode();
//         var products = await response.Content.ReadFromJsonAsync<List<Product>>();

//         Assert.NotNull(products);
//         Assert.Equal(3, products.Count);
//         Assert.Contains(products, p => p.Name == "Test Product 1");
//         Assert.Contains(products, p => p.Name == "Test Product 2");
//         Assert.Contains(products, p => p.Name == "Test Product 3");
//     }

//     [Fact]
//     public async Task GetProductById_WithValidId_ReturnsProduct()
//     {
//         // Act - first get all products to get a valid ID
//         var allProductsResponse = await _client.GetAsync("/api/products");
//         var products = await allProductsResponse.Content.ReadFromJsonAsync<List<Product>>();

//         Assert.NotNull(products);
//         var firstProductId = products.First().Id;

//         // Now get the specific product
//         var response = await _client.GetAsync($"/api/products/{firstProductId}");

//         // Assert
//         response.EnsureSuccessStatusCode();
//         var product = await response.Content.ReadFromJsonAsync<Product>();

//         Assert.NotNull(product);
//         Assert.Equal(firstProductId, product.Id);
//     }

//     [Fact]
//     public async Task GetProductById_WithInvalidId_ReturnsNotFound()
//     {
//         // Act
//         var response = await _client.GetAsync("/api/products/999");

//         // Assert
//         Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
//     }

//     [Fact]
//     public async Task CreateProduct_WithValidData_ReturnsCreatedProduct()
//     {
//         // Arrange
//         var newProduct = new Product
//         {
//             Name = "New Integration Test Product",
//             Description = "Created during integration test",
//             Price = 15.99m,
//             StockQuantity = 75
//         };

//         var content = new StringContent(
//             JsonSerializer.Serialize(newProduct, _jsonOptions),
//             Encoding.UTF8,
//             "application/json");

//         // Act
//         var response = await _client.PostAsync("/api/products", content);

//         // Assert
//         response.EnsureSuccessStatusCode();
//         Assert.Equal(HttpStatusCode.Created, response.StatusCode);

//         var createdProduct = await response.Content.ReadFromJsonAsync<Product>();
//         Assert.NotNull(createdProduct);
//         Assert.NotEqual(0, createdProduct.Id);
//         Assert.Equal(newProduct.Name, createdProduct.Name);
//         Assert.Equal(newProduct.Price, createdProduct.Price);
//     }

//     [Fact]
//     public async Task UpdateProduct_WithValidData_ReturnsUpdatedProduct()
//     {
//         // Arrange - first get a product to update
//         var allProductsResponse = await _client.GetAsync("/api/products");
//         var products = await allProductsResponse.Content.ReadFromJsonAsync<List<Product>>();

//         Assert.NotNull(products);
//         var productToUpdate = products.First();

//         productToUpdate.Name = "Updated Product Name";
//         productToUpdate.Price = 29.99m;

//         var content = new StringContent(
//             JsonSerializer.Serialize(productToUpdate, _jsonOptions),
//             Encoding.UTF8,
//             "application/json");

//         // Act
//         var response = await _client.PutAsync($"/api/products/{productToUpdate.Id}", content);

//         // Assert
//         response.EnsureSuccessStatusCode();
//         var updatedProduct = await response.Content.ReadFromJsonAsync<Product>();

//         Assert.NotNull(updatedProduct);
//         Assert.Equal(productToUpdate.Id, updatedProduct.Id);
//         Assert.Equal("Updated Product Name", updatedProduct.Name);
//         Assert.Equal(29.99m, updatedProduct.Price);
//     }

//     [Fact]
//     public async Task DeleteProduct_WithValidId_ReturnsNoContent()
//     {
//         // Arrange - create a product to delete
//         var newProduct = new Product
//         {
//             Name = "Product to Delete",
//             Price = 9.99m,
//             StockQuantity = 10
//         };

//         var content = new StringContent(
//             JsonSerializer.Serialize(newProduct, _jsonOptions),
//             Encoding.UTF8,
//             "application/json");

//         var createResponse = await _client.PostAsync("/api/products", content);
//         createResponse.EnsureSuccessStatusCode();

//         var createdProduct = await createResponse.Content.ReadFromJsonAsync<Product>();
//         Assert.NotNull(createdProduct);

//         // Act
//         var deleteResponse = await _client.DeleteAsync($"/api/products/{createdProduct.Id}");

//         // Assert
//         Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

//         // Verify it's deleted
//         var getResponse = await _client.GetAsync($"/api/products/{createdProduct.Id}");
//         Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
//     }
// }