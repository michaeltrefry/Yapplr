using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.Models;

namespace Yapplr.Api.Tests;

public class TestYapplrDbContext : YapplrDbContext
{
    public TestYapplrDbContext() : base(CreateInMemoryOptions()) { }

    public TestYapplrDbContext(DbContextOptions<YapplrDbContext> options) : base(options) { }

    private static DbContextOptions<YapplrDbContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore the problematic NotificationHistory.Data property for tests
        modelBuilder.Entity<NotificationHistory>()
            .Ignore(nh => nh.Data);
    }
}