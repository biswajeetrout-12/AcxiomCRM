using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Implementations;
using AcxiomCRM.Tests.TestHelpers;

namespace AcxiomCRM.Tests.Unit
{
    public class FollowUpServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly MockCurrentUserService _currentUserService;
        private readonly AuditService _auditService;
        private readonly FollowUpService _followUpService;
        private readonly Customer _defaultCustomer;

        public FollowUpServiceTests()
        {
            _context = TestDbContextFactory.CreateContext();
            _currentUserService = new MockCurrentUserService();
            _auditService = new AuditService(_context, _currentUserService, NullLogger<AuditService>.Instance);
            _followUpService = new FollowUpService(_context, _currentUserService, _auditService);

            _defaultCustomer = new Customer
            {
                CustomerCode = "CUST-500",
                CustomerName = "FollowUp Test Client",
                Email = "fu@client.com",
                Phone = "1231231234",
                Status = "Active",
                CreatedDate = DateTime.UtcNow
            };
            _context.Customers.Add(_defaultCustomer);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task CreateFollowUpAsync_WithPastDateForPlannedStatus_IsRejected()
        {
            // Arrange
            var fuPast = new FollowUp
            {
                CustomerId = _defaultCustomer.CustomerId,
                RelatedType = "Customer",
                FollowUpType = "Call",
                Subject = "Quarterly Check-in",
                FollowUpDate = DateTime.Today.AddDays(-2),
                Status = "Planned"
            };

            // Act
            var (success, errorMessage, created) = await _followUpService.CreateFollowUpAsync(fuPast);

            // Assert
            Assert.False(success);
            Assert.Contains("earlier than today", errorMessage);
            Assert.Null(created);
        }

        [Fact]
        public async Task CreateFollowUpAsync_WithTodayOrFutureDate_SucceedsAndAudits()
        {
            // Arrange
            var fuFuture = new FollowUp
            {
                CustomerId = _defaultCustomer.CustomerId,
                RelatedType = "Customer",
                FollowUpType = "Meeting",
                Subject = "Product Demo Presentation",
                FollowUpDate = DateTime.Today.AddDays(3),
                Status = "Planned"
            };

            // Act
            var (success, errorMessage, created) = await _followUpService.CreateFollowUpAsync(fuFuture);

            // Assert
            Assert.True(success);
            Assert.Null(errorMessage);
            Assert.NotNull(created);
            Assert.Equal("Product Demo Presentation", created.Subject);

            var audit = _context.AuditLogs.FirstOrDefault(a => a.RecordId == created.FollowUpId.ToString());
            Assert.NotNull(audit);
            Assert.Equal("FollowUp Scheduled", audit.Action);
        }

        [Fact]
        public async Task CompleteFollowUpAsync_SetsStatusCompletedAndLogsActivity()
        {
            // Arrange
            var fu = new FollowUp
            {
                CustomerId = _defaultCustomer.CustomerId,
                RelatedType = "Customer",
                FollowUpType = "Call",
                Subject = "Contract Negotiations",
                FollowUpDate = DateTime.Today,
                Status = "Planned",
                CreatedDate = DateTime.UtcNow
            };
            await _context.FollowUps.AddAsync(fu);
            await _context.SaveChangesAsync();

            // Act
            var (success, errorMessage) = await _followUpService.CompleteFollowUpAsync(fu.FollowUpId, "Client agreed to send LOI");

            // Assert
            Assert.True(success);
            Assert.Null(errorMessage);

            var updated = await _context.FollowUps.FindAsync(fu.FollowUpId);
            Assert.NotNull(updated);
            Assert.Equal("Completed", updated.Status);
            Assert.Contains("Client agreed to send LOI", updated.Remarks);

            // Verify auto-created Activity record
            var activity = _context.Activities.FirstOrDefault(a => a.CustomerId == _defaultCustomer.CustomerId);
            Assert.NotNull(activity);
            Assert.Equal("Call", activity.ActivityType);
            Assert.Contains("Contract Negotiations", activity.Subject);
        }

        [Fact]
        public async Task GetFollowUpsAsync_UpcomingAndOverdueFiltering_WorksCorrectly()
        {
            // Arrange
            var overdue = new FollowUp
            {
                CustomerId = _defaultCustomer.CustomerId,
                RelatedType = "Customer",
                FollowUpType = "Call",
                Subject = "Past Missed Task",
                FollowUpDate = DateTime.UtcNow.AddDays(-3),
                Status = "Planned",
                CreatedDate = DateTime.UtcNow.AddDays(-5)
            };
            var upcoming = new FollowUp
            {
                CustomerId = _defaultCustomer.CustomerId,
                RelatedType = "Customer",
                FollowUpType = "Email",
                Subject = "Future Task",
                FollowUpDate = DateTime.UtcNow.AddDays(5),
                Status = "Planned",
                CreatedDate = DateTime.UtcNow
            };
            await _context.FollowUps.AddRangeAsync(overdue, upcoming);
            await _context.SaveChangesAsync();

            // Act
            var overdueList = await _followUpService.GetFollowUpsAsync(overdueOnly: true);
            var upcomingList = await _followUpService.GetFollowUpsAsync(upcomingOnly: true);

            // Assert
            Assert.Contains(overdueList, f => f.Subject == "Past Missed Task");
            Assert.DoesNotContain(overdueList, f => f.Subject == "Future Task");

            Assert.Contains(upcomingList, f => f.Subject == "Future Task");
            Assert.DoesNotContain(upcomingList, f => f.Subject == "Past Missed Task");
        }
    }
}
