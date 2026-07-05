using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Tests.Helpers;

/// <summary>
/// Creates a fresh in-memory database for each test.
/// </summary>
public static class TestDbContextFactory
{
    public static InMemoryApplicationDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<InMemoryApplicationDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new InMemoryApplicationDbContext(options);
    }
}
