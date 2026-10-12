using Microsoft.Data.Sqlite;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Implementations;
using AcxiomCRM.Tests.TestHelpers;
using AcxiomCRM.ViewModels.Reports;

namespace AcxiomCRM.Tests.Unit
{
    public class DashboardAndReportsTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly MockCurrentUserService _currentUserService;
        private readonly DashboardService _dashboardService;
        private readonly ReportService _reportService;

        public DashboardAndReportsTests()
        {
            _context = TestDbContextFactory.CreateContext();
            _currentUserService = new MockCurrentUserService();
            _dashboardService = new DashboardService(_context, _currentUserService);
            _reportService = new ReportService(_context, _currentUserService);

            SeedTestData();
        }

        private void SeedTestData()
        {
            var user1 = new ApplicationUser { Id = "rep-1", UserName = "rep1@crm.com", FullName = "Alice Rep", Email = "rep1@crm.com" };
            var user2 = new ApplicationUser { Id = "rep-2", UserName = "rep2@crm.com", FullName = "Bob Rep", Email = "rep2@crm.com" };
            _context.Users.AddRange(user1, user2);

            var c1 = new Customer { CustomerCode = "C-1", CustomerName = "Corp 1", Email = "c1@c.com", Phone = "111", Status = "Active", OwnerId = "rep-1", CreatedDate = DateTime.UtcNow };
            var c2 = new Customer { CustomerCode = "C-2", CustomerName = "Corp 2", Email = "c2@c.com", Phone = "222", Status = "Active", OwnerId = "rep-2", CreatedDate = DateTime.UtcNow };
            _context.Customers.AddRange(c1, c2);

            var l1 = new Lead { LeadCode = "L-1", LeadName = "Lead 1", Email = "l1@l.com", Phone = "333", Source = "Website", Status = "Qualified", Priority = "High", ExpectedValue = 20000m, AssignedTo = "rep-1", CreatedDate = DateTime.UtcNow };
            var l2 = new Lead { LeadCode = "L-2", LeadName = "Lead 2", Email = "l2@l.com", Phone = "444", Source = "Referral", Status = "Converted", Priority = "Medium", ExpectedValue = 50000m, AssignedTo = "rep-2", CreatedDate = DateTime.UtcNow };
            _context.Leads.AddRange(l1, l2);

            var o1 = new Opportunity { OpportunityName = "Opp 1", Customer = c1, Amount = 40000m, Stage = "Proposal", Probability = 50, Status = "Open", AssignedTo = "rep-1", ExpectedCloseDate = DateTime.Today.AddDays(10), CreatedDate = DateTime.UtcNow };
            var o2 = new Opportunity { OpportunityName = "Opp 2", Customer = c2, Amount = 60000m, Stage = "Negotiation", Probability = 80, Status = "Open", AssignedTo = "rep-2", ExpectedCloseDate = DateTime.Today.AddDays(15), CreatedDate = DateTime.UtcNow };
            _context.Opportunities.AddRange(o1, o2);

            var act1 = new Activity { Customer = c1, ActivityType = "Call", Subject = "Call with Alice", Status = "Completed", ActivityDate = DateTime.UtcNow, AssignedTo = "rep-1", CreatedDate = DateTime.UtcNow };
            var act2 = new Activity { Customer = c2, ActivityType = "Meeting", Subject = "Meeting with Bob", Status = "Completed", ActivityDate = DateTime.UtcNow, AssignedTo = "rep-2", CreatedDate = DateTime.UtcNow };
            _context.Activities.AddRange(act1, act2);

            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task DashboardService_AsAdmin_ReturnsSystemWideKpis()
        {
            // Arrange
            _currentUserService.SetUser("admin-1", "admin@acxiom.com", "Admin");

            // Act
            var vm = await _dashboardService.GetDashboardDataAsync();

            // Assert
            Assert.Equal(2, vm.TotalCustomers);
            Assert.Equal(2, vm.TotalLeads);
            Assert.Equal(1, vm.OpenLeads); // L-1 is Qualified, L-2 is Converted
            Assert.Equal(2, vm.OpenOpportunities);
            Assert.Equal(100000m, vm.TotalPipelineValue); // 40k + 60k
        }

        [Fact]
        public async Task DashboardService_AsSalesExecutive_ReturnsOnlyAssignedKpis()
        {
            // Arrange
            _currentUserService.SetUser("rep-1", "rep1@crm.com", "SalesExecutive");

            // Act
            var vm = await _dashboardService.GetDashboardDataAsync();

            // Assert
            Assert.Equal(1, vm.TotalCustomers); // Only c1
            Assert.Equal(1, vm.TotalLeads);     // Only l1
            Assert.Equal(1, vm.OpenOpportunities); // Only o1
            Assert.Equal(40000m, vm.TotalPipelineValue); // Only 40k
        }

        [Fact]
        public async Task ReportService_PipelineReport_CalculatesWeightedAmountCorrectly()
        {
            // Arrange
            _currentUserService.SetUser("admin-1", "admin@acxiom.com", "Admin");
            var filter = new ReportFilterViewModel { ReportType = "Pipeline" };

            // Act
            var report = await _reportService.GenerateReportAsync(filter);

            // Assert
            Assert.NotEmpty(report.PipelineItems);
            // o1: 40000 * 50% = 20000 weighted
            // o2: 60000 * 80% = 48000 weighted
            var totalWeighted = report.PipelineItems.Sum(p => p.WeightedAmount);
            Assert.Equal(68000m, totalWeighted);
        }

        [Fact]
        public async Task ReportService_ExportCsv_SanitizesFormulaInjection()
        {
            // Arrange: insert customer with dangerous spreadsheet formula name
            var dangerousCustomer = new Customer
            {
                CustomerCode = "CUST-HACK",
                CustomerName = "=cmd|'/c calc'!A0", // DDE / Formula injection vector
                Email = "+safe_email@domain.com",   // Starts with +
                Phone = "-1234567890",              // Starts with -
                CompanyName = "@malicious_link",    // Starts with @
                Status = "Active",
                CreatedDate = DateTime.UtcNow
            };
            await _context.Customers.AddAsync(dangerousCustomer);
            await _context.SaveChangesAsync();

            _currentUserService.SetUser("admin-1", "admin@acxiom.com", "Admin");
            var filter = new ReportFilterViewModel { ReportType = "Customers" };

            // Act
            var csvBytes = await _reportService.ExportReportCsvAsync(filter);
            var csvString = System.Text.Encoding.UTF8.GetString(csvBytes);

            // Assert: verify that leading =, +, -, @ were neutralized with a single quote prefix
            Assert.Contains("'=cmd|'/c calc'!A0", csvString);
            Assert.Contains("'+safe_email@domain.com", csvString);
            Assert.Contains("'-1234567890", csvString);
            Assert.Contains("'@malicious_link", csvString);
        }
    }
}
