using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Implementations;
using AcxiomCRM.Tests.TestHelpers;

namespace AcxiomCRM.Tests.Unit
{
    public class CustomerServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly MockCurrentUserService _currentUserService;
        private readonly AuditService _auditService;
        private readonly CustomerService _customerService;

        public CustomerServiceTests()
        {
            _context = TestDbContextFactory.CreateContext();
            _currentUserService = new MockCurrentUserService();
            _auditService = new AuditService(_context, _currentUserService, NullLogger<AuditService>.Instance);
            _customerService = new CustomerService(_context, _currentUserService, _auditService);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task CreateCustomerAsync_WithValidData_SucceedsAndGeneratesCodeAndAudit()
        {
            // Arrange
            var customer = new Customer
            {
                CustomerName = "Enterprise Global",
                Email = "contact@enterprise.com",
                Phone = "9876543210",
                CompanyName = "Enterprise Inc",
                Status = "Active"
            };

            // Act
            var (success, errorMessage, created) = await _customerService.CreateCustomerAsync(customer);

            // Assert
            Assert.True(success);
            Assert.Null(errorMessage);
            Assert.NotNull(created);
            Assert.StartsWith("CUST-", created.CustomerCode);
            Assert.Equal("Enterprise Global", created.CustomerName);

            // Verify Audit
            var audit = _context.AuditLogs.FirstOrDefault(a => a.RecordId == created.CustomerId.ToString());
            Assert.NotNull(audit);
            Assert.Equal("Customer Created", audit.Action);
            Assert.Equal("Customer", audit.EntityName);
        }

        [Fact]
        public async Task CreateCustomerAsync_WithDuplicateEmail_FailsWithDuplicateMessage()
        {
            // Arrange
            var first = new Customer
            {
                CustomerCode = "CUST-1001",
                CustomerName = "First Client",
                Email = "duplicate@company.com",
                Phone = "1111111111",
                Status = "Active",
                CreatedDate = DateTime.UtcNow
            };
            await _context.Customers.AddAsync(first);
            await _context.SaveChangesAsync();

            var duplicate = new Customer
            {
                CustomerName = "Second Client",
                Email = "DUPLICATE@company.com", // Case insensitive test
                Phone = "2222222222",
                Status = "Active"
            };

            // Act
            var (success, errorMessage, created) = await _customerService.CreateCustomerAsync(duplicate);

            // Assert
            Assert.False(success);
            Assert.Contains("email address already exists", errorMessage);
            Assert.Null(created);
        }

        [Fact]
        public async Task CreateCustomerAsync_WithDuplicatePhone_FailsWithDuplicateMessage()
        {
            // Arrange
            var first = new Customer
            {
                CustomerCode = "CUST-1001",
                CustomerName = "First Client",
                Email = "unique1@company.com",
                Phone = "9998887776",
                Status = "Active",
                CreatedDate = DateTime.UtcNow
            };
            await _context.Customers.AddAsync(first);
            await _context.SaveChangesAsync();

            var duplicate = new Customer
            {
                CustomerName = "Second Client",
                Email = "unique2@company.com",
                Phone = "9998887776",
                Status = "Active"
            };

            // Act
            var (success, errorMessage, created) = await _customerService.CreateCustomerAsync(duplicate);

            // Assert
            Assert.False(success);
            Assert.Contains("phone number already exists", errorMessage);
            Assert.Null(created);
        }

        [Fact]
        public async Task CreateCustomerAsync_AsSalesExecutive_AutoAssignsOwnerId()
        {
            // Arrange
            _currentUserService.SetUser("sales-user-42", "sales@acxiom.com", "SalesExecutive");
            var customer = new Customer
            {
                CustomerName = "Lead Customer",
                Email = "leadcust@test.com",
                Phone = "9870001111",
                Status = "Active"
            };

            // Act
            var (success, _, created) = await _customerService.CreateCustomerAsync(customer);

            // Assert
            Assert.True(success);
            Assert.NotNull(created);
            Assert.Equal("sales-user-42", created.OwnerId);
        }

        [Fact]
        public async Task UpdateCustomerAsync_AsSalesExecutive_WhenNotOwner_IsRejected()
        {
            // Arrange
            var existing = new Customer
            {
                CustomerCode = "CUST-2001",
                CustomerName = "Other Rep's Account",
                Email = "rep1@corp.com",
                Phone = "1231231234",
                Status = "Active",
                OwnerId = "rep-user-99",
                CreatedDate = DateTime.UtcNow
            };
            await _context.Customers.AddAsync(existing);
            await _context.SaveChangesAsync();

            _currentUserService.SetUser("different-rep-10", "diff@acxiom.com", "SalesExecutive");

            var updateDto = new Customer
            {
                CustomerId = existing.CustomerId,
                CustomerName = "Tampered Account Name",
                Email = "rep1@corp.com",
                Phone = "1231231234",
                Status = "Active"
            };

            // Act
            var (success, errorMessage) = await _customerService.UpdateCustomerAsync(updateDto);

            // Assert
            Assert.False(success);
            Assert.Contains("not authorized", errorMessage);
        }

        [Fact]
        public async Task DeleteOrDeactivateCustomerAsync_AsSalesExecutive_IsRejected()
        {
            // Arrange
            var existing = new Customer
            {
                CustomerCode = "CUST-3001",
                CustomerName = "Protected Corp",
                Email = "prot@corp.com",
                Phone = "5555555555",
                Status = "Active",
                CreatedDate = DateTime.UtcNow
            };
            await _context.Customers.AddAsync(existing);
            await _context.SaveChangesAsync();

            _currentUserService.SetUser("rep-1", "rep@acxiom.com", "SalesExecutive");

            // Act
            var (success, errorMessage) = await _customerService.DeleteOrDeactivateCustomerAsync(existing.CustomerId);

            // Assert
            Assert.False(success);
            Assert.Contains("not authorized", errorMessage);
        }

        [Fact]
        public async Task DeleteOrDeactivateCustomerAsync_AsAdmin_SoftDeactivatesAndAudits()
        {
            // Arrange
            _currentUserService.SetUser("admin-1", "admin@acxiom.com", "Admin");
            var existing = new Customer
            {
                CustomerCode = "CUST-4001",
                CustomerName = "Acme Corp",
                Email = "acme@corp.com",
                Phone = "4444444444",
                Status = "Active",
                CreatedDate = DateTime.UtcNow
            };
            await _context.Customers.AddAsync(existing);
            await _context.SaveChangesAsync();

            // Act
            var (success, errorMessage) = await _customerService.DeleteOrDeactivateCustomerAsync(existing.CustomerId);

            // Assert
            Assert.True(success);
            Assert.Null(errorMessage);

            var updated = await _context.Customers.FindAsync(existing.CustomerId);
            Assert.NotNull(updated);
            Assert.Equal("Inactive", updated.Status);

            var audit = _context.AuditLogs.FirstOrDefault(a => a.RecordId == existing.CustomerId.ToString() && a.Action == "Customer Deactivated");
            Assert.NotNull(audit);
            Assert.Equal("Success", audit.Result);
        }
    }
}
