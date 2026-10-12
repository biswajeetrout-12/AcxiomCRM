using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Implementations;
using AcxiomCRM.Tests.TestHelpers;

namespace AcxiomCRM.Tests.Unit
{
    public class LeadServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly MockCurrentUserService _currentUserService;
        private readonly AuditService _auditService;
        private readonly LeadService _leadService;

        public LeadServiceTests()
        {
            _context = TestDbContextFactory.CreateContext();
            _currentUserService = new MockCurrentUserService();
            _auditService = new AuditService(_context, _currentUserService, NullLogger<AuditService>.Instance);
            _leadService = new LeadService(_context, _currentUserService, _auditService);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task CreateLeadAsync_WithValidData_SucceedsAndGeneratesCodeAndAudit()
        {
            // Arrange
            var lead = new Lead
            {
                LeadName = "Sarah Connor",
                Email = "sarah@cyberdyne.com",
                Phone = "9988776655",
                CompanyName = "Cyberdyne Systems",
                Source = "Website",
                Status = "New",
                Priority = "High",
                ExpectedValue = 50000m
            };

            // Act
            var (success, errorMessage, created) = await _leadService.CreateLeadAsync(lead);

            // Assert
            Assert.True(success);
            Assert.Null(errorMessage);
            Assert.NotNull(created);
            Assert.StartsWith("LEAD-", created.LeadCode);
            Assert.Equal("Cyberdyne Systems", created.CompanyName);

            var audit = _context.AuditLogs.FirstOrDefault(a => a.RecordId == created.LeadId.ToString());
            Assert.NotNull(audit);
            Assert.Equal("Lead Created", audit.Action);
        }

        [Fact]
        public async Task CreateLeadAsync_WithNegativeExpectedValue_IsRejected()
        {
            // Arrange
            var lead = new Lead
            {
                LeadName = "Bad Value Lead",
                Email = "bad@lead.com",
                Phone = "1234567890",
                Source = "Referral",
                Status = "New",
                Priority = "Medium",
                ExpectedValue = -500m
            };

            // Act
            var (success, errorMessage, created) = await _leadService.CreateLeadAsync(lead);

            // Assert
            Assert.False(success);
            Assert.Contains("Expected Value cannot be negative", errorMessage);
            Assert.Null(created);
        }

        [Fact]
        public async Task UpdateLeadAsync_AsSalesExecutive_WhenNotOwner_IsRejected()
        {
            // Arrange
            var existing = new Lead
            {
                LeadCode = "LEAD-101",
                LeadName = "Target Client",
                Email = "target@corp.com",
                Phone = "1112223334",
                Source = "Cold Call",
                Status = "Contacted",
                Priority = "Low",
                AssignedTo = "rep-alice",
                CreatedDate = DateTime.UtcNow
            };
            await _context.Leads.AddAsync(existing);
            await _context.SaveChangesAsync();

            _currentUserService.SetUser("rep-bob", "bob@acxiom.com", "SalesExecutive");

            var updated = new Lead
            {
                LeadId = existing.LeadId,
                LeadName = "Attempted Tamper",
                Email = "target@corp.com",
                Phone = "1112223334",
                Source = "Cold Call",
                Status = "Qualified",
                Priority = "High"
            };

            // Act
            var (success, errorMessage) = await _leadService.UpdateLeadAsync(updated);

            // Assert
            Assert.False(success);
            Assert.Contains("not authorized", errorMessage);
        }

        [Fact]
        public async Task DeleteLeadAsync_AsSalesExecutive_IsBlocked()
        {
            // Arrange
            var existing = new Lead
            {
                LeadCode = "LEAD-202",
                LeadName = "Delete Target",
                Email = "del@lead.com",
                Phone = "2223334445",
                Source = "Event",
                Status = "New",
                Priority = "Low",
                AssignedTo = "rep-1",
                CreatedDate = DateTime.UtcNow
            };
            await _context.Leads.AddAsync(existing);
            await _context.SaveChangesAsync();

            _currentUserService.SetUser("rep-1", "rep@acxiom.com", "SalesExecutive");

            // Act
            var (success, errorMessage) = await _leadService.DeleteLeadAsync(existing.LeadId);

            // Assert
            Assert.False(success);
            Assert.Contains("not authorized to delete leads", errorMessage);
        }

        [Fact]
        public async Task ConvertLeadAsync_WithValidOpportunity_CreatesCustomerAndOpportunityAndUpdatesLead()
        {
            // Arrange
            var lead = new Lead
            {
                LeadCode = "LEAD-303",
                LeadName = "Tech Start Lead",
                Email = "founder@techstart.io",
                Phone = "4445556667",
                CompanyName = "TechStart Innovations",
                Source = "Organic Search",
                Status = "Qualified",
                Priority = "High",
                ExpectedValue = 75000m,
                CreatedDate = DateTime.UtcNow
            };
            await _context.Leads.AddAsync(lead);
            await _context.SaveChangesAsync();

            // Act
            var (success, errorMessage, customerId) = await _leadService.ConvertLeadAsync(
                lead.LeadId,
                opportunityAmount: 75000m,
                opportunityName: "TechStart Enterprise License");

            // Assert
            Assert.True(success);
            Assert.Null(errorMessage);
            Assert.NotNull(customerId);

            // Verify Customer
            var customer = await _context.Customers.FindAsync(customerId.Value);
            Assert.NotNull(customer);
            Assert.Equal("TechStart Innovations", customer.CustomerName);
            Assert.Equal("founder@techstart.io", customer.Email);

            // Verify Lead is marked Converted
            var convertedLead = await _context.Leads.FindAsync(lead.LeadId);
            Assert.NotNull(convertedLead);
            Assert.Equal("Converted", convertedLead.Status);
            Assert.Equal(customerId.Value, convertedLead.ConvertedCustomerId);

            // Verify Opportunity was created
            var opp = _context.Opportunities.FirstOrDefault(o => o.CustomerId == customerId.Value);
            Assert.NotNull(opp);
            Assert.Equal("TechStart Enterprise License", opp.OpportunityName);
            Assert.Equal(75000m, opp.Amount);
            Assert.Equal("Qualification", opp.Stage);
            Assert.Equal(25, opp.Probability);

            // Verify Conversion Audit
            var audit = _context.AuditLogs.FirstOrDefault(a => a.RecordId == lead.LeadId.ToString() && a.Action == "Lead Converted");
            Assert.NotNull(audit);
        }

        [Fact]
        public async Task ConvertLeadAsync_WhenAlreadyConverted_IsRejected()
        {
            // Arrange
            var lead = new Lead
            {
                LeadCode = "LEAD-404",
                LeadName = "Already Converted Lead",
                Email = "converted@client.com",
                Phone = "7778889990",
                CompanyName = "Converted Corp",
                Source = "Website",
                Status = "Converted",
                Priority = "Medium",
                ConvertedCustomerId = 105,
                CreatedDate = DateTime.UtcNow
            };
            await _context.Leads.AddAsync(lead);
            await _context.SaveChangesAsync();

            // Act
            var (success, errorMessage, _) = await _leadService.ConvertLeadAsync(lead.LeadId, 10000m);

            // Assert
            Assert.False(success);
            Assert.Contains("already been converted", errorMessage);
        }
    }
}
