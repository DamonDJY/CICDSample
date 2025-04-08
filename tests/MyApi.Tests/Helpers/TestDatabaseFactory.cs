using Endpoint.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MyApi.Tests.Helpers;

public class TestDatabaseFactory
{
    public static ApplicationDbContext CreateTestDatabase()
    {
        // Create and open a connection. This creates the SQLite in-memory database, which will persist until the connection is closed
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        // These options will be used by the context instances
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;

        // Create the schema and seed some data
        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }
}