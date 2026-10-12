using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AcxiomCRM.Authorization;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.Tests.Unit
{
    public class FreshDatabaseInitializationTests
    {
        private ServiceProvider BuildServiceProvider(string dbName)
        {
            var services = new ServiceCollection();

            services.AddLogging(builder => builder.AddDebug());

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(dbName));

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            return services.BuildServiceProvider();
        }

        [Fact]
        public async Task FreshDatabase_InitializesAndSeedsAllThreeRolesAndDemoAccounts()
        {
            // Arrange: brand new database
            var dbName = "FreshTestDb_" + Guid.NewGuid();
            using var serviceProvider = BuildServiceProvider(dbName);

            // Act: Run SeedAsync on empty database
            await DbInitializer.SeedAsync(serviceProvider, seedDemoData: true);

            // Assert
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. Roles exist
            Assert.True(await roleManager.RoleExistsAsync(Roles.Admin));
            Assert.True(await roleManager.RoleExistsAsync(Roles.Manager));
            Assert.True(await roleManager.RoleExistsAsync(Roles.SalesExecutive));

            // 2. Admin account exists, role is Admin, and password is valid
            var admin = await userManager.FindByEmailAsync("admin@acxiomcrm.com");
            Assert.NotNull(admin);
            Assert.True(admin.IsActive);
            Assert.True(await userManager.IsInRoleAsync(admin, Roles.Admin));
            Assert.True(await userManager.CheckPasswordAsync(admin, "Admin@123"));

            // 3. Manager account exists, role is Manager, and password is valid
            var manager = await userManager.FindByEmailAsync("manager@acxiomcrm.com");
            Assert.NotNull(manager);
            Assert.True(manager.IsActive);
            Assert.True(await userManager.IsInRoleAsync(manager, Roles.Manager));
            Assert.True(await userManager.CheckPasswordAsync(manager, "Manager@123"));

            // 4. Sales Executive account exists, role is SalesExecutive, and password is valid
            var sales = await userManager.FindByEmailAsync("sales@acxiomcrm.com");
            Assert.NotNull(sales);
            Assert.True(sales.IsActive);
            Assert.True(await userManager.IsInRoleAsync(sales, Roles.SalesExecutive));
            Assert.True(await userManager.CheckPasswordAsync(sales, "Sales@123"));

            // 5. Baseline CRM entities exist
            Assert.True(await context.Customers.AnyAsync());
            Assert.True(await context.Leads.AnyAsync());
            Assert.True(await context.Opportunities.AnyAsync());
            Assert.True(await context.FollowUps.AnyAsync());
            Assert.True(await context.AuditLogs.AnyAsync());
        }

        [Fact]
        public async Task RepeatedInitialization_IsIdempotent_PreservesDataAndPasswords()
        {
            // Arrange
            var dbName = "IdempotentTestDb_" + Guid.NewGuid();
            using var serviceProvider = BuildServiceProvider(dbName);

            // Act 1: Initial seed
            await DbInitializer.SeedAsync(serviceProvider, seedDemoData: true);

            using (var scope1 = serviceProvider.CreateScope())
            {
                var context1 = scope1.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var userManager1 = scope1.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                var initialUserCount = await userManager1.Users.CountAsync();
                var initialCustomerCount = await context1.Customers.CountAsync();

                // Change admin password to custom
                var admin = await userManager1.FindByEmailAsync("admin@acxiomcrm.com");
                Assert.NotNull(admin);
                var token = await userManager1.GeneratePasswordResetTokenAsync(admin);
                var changeRes = await userManager1.ResetPasswordAsync(admin, token, "CustomAdmin@456");
                Assert.True(changeRes.Succeeded);

                // Act 2: Run SeedAsync second time
                await DbInitializer.SeedAsync(serviceProvider, seedDemoData: true);

                // Assert: User counts and customer counts must NOT duplicate
                var finalUserCount = await userManager1.Users.CountAsync();
                var finalCustomerCount = await context1.Customers.CountAsync();

                Assert.Equal(initialUserCount, finalUserCount);
                Assert.Equal(initialCustomerCount, finalCustomerCount);

                // Existing custom password must NOT be overwritten
                var refreshedAdmin = await userManager1.FindByEmailAsync("admin@acxiomcrm.com");
                Assert.NotNull(refreshedAdmin);
                Assert.True(await userManager1.CheckPasswordAsync(refreshedAdmin, "CustomAdmin@456"));
                Assert.False(await userManager1.CheckPasswordAsync(refreshedAdmin, "Admin@123"));
            }
        }

        [Fact]
        public async Task WhenDemoSeedingIsDisabled_RolesAreCreatedButDemoUsersAreNot()
        {
            // Arrange: simulating production deployment
            var dbName = "ProductionSimDb_" + Guid.NewGuid();
            using var serviceProvider = BuildServiceProvider(dbName);

            // Act: seedDemoData = false
            await DbInitializer.SeedAsync(serviceProvider, seedDemoData: false);

            // Assert
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Roles must exist for authorization
            Assert.True(await roleManager.RoleExistsAsync(Roles.Admin));
            Assert.True(await roleManager.RoleExistsAsync(Roles.Manager));
            Assert.True(await roleManager.RoleExistsAsync(Roles.SalesExecutive));

            // Demo accounts must NOT be created
            Assert.Null(await userManager.FindByEmailAsync("admin@acxiomcrm.com"));
            Assert.Null(await userManager.FindByEmailAsync("manager@acxiomcrm.com"));
            Assert.Null(await userManager.FindByEmailAsync("sales@acxiomcrm.com"));

            // Sample CRM data must NOT be created
            Assert.False(await context.Customers.AnyAsync());
            Assert.False(await context.Leads.AnyAsync());
            Assert.False(await context.Opportunities.AnyAsync());
        }
    }
}
