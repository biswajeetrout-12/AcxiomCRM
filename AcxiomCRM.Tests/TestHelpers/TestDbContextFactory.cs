using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;

namespace AcxiomCRM.Tests.TestHelpers
{
    public static class TestDbContextFactory
    {
        public static ApplicationDbContext CreateContext(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();

            return context;
        }
    }
}
