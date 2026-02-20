using Microsoft.EntityFrameworkCore;
using WorkScholarship.Infrastructure.Data;

namespace WorkScholarship.Application.Tests.TestHelpers;

/// <summary>
/// Factory for creating InMemory EF Core DbContext instances for testing.
/// Each test gets its own isolated database to prevent test interference.
/// </summary>
public static class DbContextFactory
{
    /// <summary>
    /// Creates a new ApplicationDbContext backed by an InMemory database with a unique name.
    /// </summary>
    /// <param name="databaseName">Optional name for the database. If not provided, a unique GUID is used.</param>
    /// <returns>A new ApplicationDbContext instance with an empty InMemory database.</returns>
    public static ApplicationDbContext CreateInMemoryContext(string? databaseName = null)
    {
        var dbName = databaseName ?? Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new ApplicationDbContext(options);
    }
}
