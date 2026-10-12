using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Implementations;
using AcxiomCRM.Tests.TestHelpers;

namespace AcxiomCRM.Tests.Unit
{
    public class ActivityAndAuditServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly MockCurrentUserService _currentUserService;
        private readonly AuditService _auditService;
        private readonly ActivityService _activityService;

        public ActivityAndAuditServiceTests()
        {
            _context = TestDbContextFactory.CreateContext();
            _currentUserService = new MockCurrentUserService();
            _auditService = new AuditService(_context, _currentUserService, NullLogger<AuditService>.Instance);
            _activityService = new ActivityService(_context, _currentUserService, _auditService);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task CreateActivityAsync_WithCustomerAssociation_SucceedsAndCreatesAudit()
        {
            // Arrange
            var customer = new Customer
            {
                CustomerCode = "CUST-ACT",
                CustomerName = "Activity Target Customer",
                Email = "act@cust.com",
                Phone = "1234567890",
                Status = "Active",
                CreatedDate = DateTime.UtcNow
            };
            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();

            var activity = new Activity
            {
                CustomerId = customer.CustomerId,
                ActivityType = "Meeting",
                Subject = "Architecture Review Session",
                Description = "Discussed CRM migration timeline",
                ActivityDate = DateTime.UtcNow,
                Status = "Completed"
            };

            // Act
            var (success, errorMessage, created) = await _activityService.CreateActivityAsync(activity);

            // Assert
            Assert.True(success);
            Assert.Null(errorMessage);
            Assert.NotNull(created);
            Assert.Equal("Architecture Review Session", created.Subject);
            Assert.Equal(customer.CustomerId, created.CustomerId);

            var audit = _context.AuditLogs.FirstOrDefault(a => a.RecordId == created.ActivityId.ToString() && a.Action == "Activity Logged");
            Assert.NotNull(audit);
            Assert.Equal("Activity", audit.EntityName);
        }

        [Fact]
        public async Task AuditService_LogAsync_CapturesAllSecurityMetadata()
        {
            // Arrange
            _currentUserService.SetUser("audit-user-88", "sec-admin@acxiom.com", "Admin");
            _currentUserService.IpAddress = "192.168.1.100";

            // Act
            await _auditService.LogAsync(
                action: "Security Role Changed",
                entityName: "User",
                recordId: "target-user-01",
                oldValue: "Role: SalesExecutive",
                newValue: "Role: Manager",
                result: "Success",
                details: "Promoted to Regional Sales Manager");

            // Assert
            var log = _context.AuditLogs.FirstOrDefault(a => a.RecordId == "target-user-01");
            Assert.NotNull(log);
            Assert.Equal("audit-user-88", log.UserId);
            Assert.Equal("Security Role Changed", log.Action);
            Assert.Equal("User", log.EntityName);
            Assert.Equal("Role: SalesExecutive", log.OldValue);
            Assert.Equal("Role: Manager", log.NewValue);
            Assert.Equal("Success", log.Result);
            Assert.Equal("192.168.1.100", log.IpAddress);
            Assert.True(log.CreatedDate <= DateTime.UtcNow);
        }
    }
}
