using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Implementations;
using AcxiomCRM.Tests.TestHelpers;

namespace AcxiomCRM.Tests.Unit
{
    public class OpportunityServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly MockCurrentUserService _currentUserService;
        private readonly AuditService _auditService;
        private readonly OpportunityService _opportunityService;
        private readonly Customer _defaultCustomer;

        public OpportunityServiceTests()
        {
            _context = TestDbContextFactory.CreateContext();
            _currentUserService = new MockCurrentUserService();
            _auditService = new AuditService(_context, _currentUserService, NullLogger<AuditService>.Instance);
            _opportunityService = new OpportunityService(_context, _currentUserService, _auditService);

            _defaultCustomer = new Customer
            {
                CustomerCode = "CUST-900",
                CustomerName = "Apex Holdings",
                Email = "info@apex.com",
                Phone = "1234567890",
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
        public async Task CreateOpportunityAsync_WithZeroOrNegativeAmount_IsRejected()
        {
            // Arrange
            var oppZero = new Opportunity
            {
                OpportunityName = "Zero Deal",
                CustomerId = _defaultCustomer.CustomerId,
                Amount = 0m,
                Stage = "Proposal",
                Probability = 50,
                ExpectedCloseDate = DateTime.Today.AddDays(15),
                Status = "Open"
            };

            // Act
            var (success, errorMessage, created) = await _opportunityService.CreateOpportunityAsync(oppZero);

            // Assert
            Assert.False(success);
            Assert.Contains("must be greater than 0", errorMessage);
            Assert.Null(created);
        }

        [Fact]
        public async Task CreateOpportunityAsync_WithInvalidProbability_IsRejected()
        {
            // Arrange
            var oppOver = new Opportunity
            {
                OpportunityName = "120% Probability Deal",
                CustomerId = _defaultCustomer.CustomerId,
                Amount = 50000m,
                Stage = "Negotiation",
                Probability = 120,
                ExpectedCloseDate = DateTime.Today.AddDays(20),
                Status = "Open"
            };

            // Act
            var (success, errorMessage, created) = await _opportunityService.CreateOpportunityAsync(oppOver);

            // Assert
            Assert.False(success);
            Assert.Contains("Probability must be between 0 and 100", errorMessage);
            Assert.Null(created);
        }

        [Fact]
        public async Task CreateOpportunityAsync_WithPastExpectedCloseDate_IsRejected()
        {
            // Arrange
            var oppPast = new Opportunity
            {
                OpportunityName = "Past Date Deal",
                CustomerId = _defaultCustomer.CustomerId,
                Amount = 25000m,
                Stage = "Qualification",
                Probability = 30,
                ExpectedCloseDate = DateTime.Today.AddDays(-5),
                Status = "Open"
            };

            // Act
            var (success, errorMessage, created) = await _opportunityService.CreateOpportunityAsync(oppPast);

            // Assert
            Assert.False(success);
            Assert.Contains("Expected Close Date cannot be in the past", errorMessage);
            Assert.Null(created);
        }

        [Fact]
        public async Task CreateOpportunityAsync_WhenStageIsWon_AutomaticallySetsStatusWonAnd100PercentProbability()
        {
            // Arrange
            var oppWon = new Opportunity
            {
                OpportunityName = "Signed Enterprise Contract",
                CustomerId = _defaultCustomer.CustomerId,
                Amount = 100000m,
                Stage = "Won",
                Probability = 50, // Should be auto-set to 100
                ExpectedCloseDate = DateTime.Today.AddDays(10),
                Status = "Open"
            };

            // Act
            var (success, _, created) = await _opportunityService.CreateOpportunityAsync(oppWon);

            // Assert
            Assert.True(success);
            Assert.NotNull(created);
            Assert.Equal("Won", created.Status);
            Assert.Equal(100, created.Probability);
        }

        [Fact]
        public async Task GetTotalPipelineValueAsync_CalculatesSumOfOpenOpportunities()
        {
            // Arrange
            var opp1 = new Opportunity
            {
                OpportunityName = "Deal 1",
                CustomerId = _defaultCustomer.CustomerId,
                Amount = 30000m,
                Stage = "Proposal",
                Probability = 50,
                ExpectedCloseDate = DateTime.Today.AddDays(30),
                Status = "Open",
                CreatedDate = DateTime.UtcNow
            };
            var opp2 = new Opportunity
            {
                OpportunityName = "Deal 2",
                CustomerId = _defaultCustomer.CustomerId,
                Amount = 70000m,
                Stage = "Negotiation",
                Probability = 80,
                ExpectedCloseDate = DateTime.Today.AddDays(15),
                Status = "Open",
                CreatedDate = DateTime.UtcNow
            };
            var oppWon = new Opportunity
            {
                OpportunityName = "Deal 3 Won",
                CustomerId = _defaultCustomer.CustomerId,
                Amount = 50000m,
                Stage = "Won",
                Probability = 100,
                ExpectedCloseDate = DateTime.Today.AddDays(5),
                Status = "Won",
                CreatedDate = DateTime.UtcNow
            };

            await _context.Opportunities.AddRangeAsync(opp1, opp2, oppWon);
            await _context.SaveChangesAsync();

            // Act
            var totalPipeline = await _opportunityService.GetTotalPipelineValueAsync();

            // Assert: only Open opportunities (30,000 + 70,000 = 100,000)
            Assert.Equal(100000m, totalPipeline);
        }

        [Fact]
        public async Task UpdateOpportunityAsync_AsSalesExecutive_WhenNotAssigned_IsRejected()
        {
            // Arrange
            var opp = new Opportunity
            {
                OpportunityName = "Assigned to Alice",
                CustomerId = _defaultCustomer.CustomerId,
                Amount = 40000m,
                Stage = "Proposal",
                Probability = 60,
                ExpectedCloseDate = DateTime.Today.AddDays(20),
                Status = "Open",
                AssignedTo = "rep-alice",
                CreatedDate = DateTime.UtcNow
            };
            await _context.Opportunities.AddAsync(opp);
            await _context.SaveChangesAsync();

            _currentUserService.SetUser("rep-bob", "bob@acxiom.com", "SalesExecutive");

            var updated = new Opportunity
            {
                OpportunityId = opp.OpportunityId,
                OpportunityName = "Tampered Deal",
                CustomerId = _defaultCustomer.CustomerId,
                Amount = 40000m,
                Stage = "Proposal",
                Probability = 60,
                ExpectedCloseDate = DateTime.Today.AddDays(20),
                Status = "Open"
            };

            // Act
            var (success, errorMessage) = await _opportunityService.UpdateOpportunityAsync(updated);

            // Assert
            Assert.False(success);
            Assert.Contains("not authorized", errorMessage);
        }
    }
}
