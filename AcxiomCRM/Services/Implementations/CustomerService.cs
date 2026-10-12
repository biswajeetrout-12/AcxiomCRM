using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Interfaces;

namespace AcxiomCRM.Services.Implementations
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditService _auditService;

        public CustomerService(
            ApplicationDbContext context,
            ICurrentUserService currentUserService,
            IAuditService auditService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _auditService = auditService;
        }

        private IQueryable<Customer> GetScopedQuery()
        {
            var query = _context.Customers
                .Include(c => c.Owner)
                .AsQueryable();

            if (_currentUserService.IsSalesExecutive)
            {
                query = query.Where(c => c.OwnerId == _currentUserService.UserId);
            }

            return query;
        }

        public async Task<IEnumerable<Customer>> GetCustomersAsync(string? search = null, string? status = null)
        {
            var query = GetScopedQuery();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(c => c.CustomerName.Contains(search) ||
                                         c.Email.Contains(search) ||
                                         c.Phone.Contains(search) ||
                                         (c.CompanyName != null && c.CompanyName.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(c => c.Status == status);
            }

            return await query.OrderByDescending(c => c.CreatedDate).ToListAsync();
        }

        public async Task<Customer?> GetCustomerByIdAsync(int id)
        {
            return await GetScopedQuery()
                .Include(c => c.Opportunities)
                .Include(c => c.FollowUps)
                .Include(c => c.Activities)
                .FirstOrDefaultAsync(c => c.CustomerId == id);
        }

        public async Task<bool> IsEmailUniqueAsync(string email, int? currentCustomerId = null)
        {
            var query = _context.Customers.Where(c => c.Email.ToLower() == email.Trim().ToLower());
            if (currentCustomerId.HasValue)
            {
                query = query.Where(c => c.CustomerId != currentCustomerId.Value);
            }
            return !await query.AnyAsync();
        }

        public async Task<bool> IsPhoneUniqueAsync(string phone, int? currentCustomerId = null)
        {
            var query = _context.Customers.Where(c => c.Phone == phone.Trim());
            if (currentCustomerId.HasValue)
            {
                query = query.Where(c => c.CustomerId != currentCustomerId.Value);
            }
            return !await query.AnyAsync();
        }

        public async Task<(bool Success, string? ErrorMessage, Customer? Customer)> CreateCustomerAsync(Customer customer)
        {
            if (!await IsEmailUniqueAsync(customer.Email))
            {
                return (false, "A customer with this email address already exists.", null);
            }

            if (!await IsPhoneUniqueAsync(customer.Phone))
            {
                return (false, "A customer with this phone number already exists.", null);
            }

            // Auto-assign owner to current user if not provided and current user is Sales Executive
            if (string.IsNullOrEmpty(customer.OwnerId) && _currentUserService.IsSalesExecutive)
            {
                customer.OwnerId = _currentUserService.UserId;
            }

            // Generate unique CustomerCode if blank
            if (string.IsNullOrWhiteSpace(customer.CustomerCode))
            {
                var maxId = await _context.Customers.MaxAsync(c => (int?)c.CustomerId) ?? 1000;
                customer.CustomerCode = $"CUST-{maxId + 1}";
            }

            customer.CreatedDate = DateTime.UtcNow;
            customer.CreatedBy = _currentUserService.UserName ?? "System";

            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "Customer Created",
                entityName: "Customer",
                recordId: customer.CustomerId.ToString(),
                newValue: $"Code: {customer.CustomerCode}, Name: {customer.CustomerName}, Email: {customer.Email}",
                result: "Success",
                details: $"Customer {customer.CustomerName} created successfully");

            return (true, null, customer);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateCustomerAsync(Customer customer)
        {
            var existing = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);
            if (existing == null)
            {
                return (false, "Customer not found.");
            }

            // Authorization check
            if (_currentUserService.IsSalesExecutive && existing.OwnerId != _currentUserService.UserId)
            {
                return (false, "You are not authorized to update this customer.");
            }

            if (!await IsEmailUniqueAsync(customer.Email, customer.CustomerId))
            {
                return (false, "Another customer with this email address already exists.");
            }

            if (!await IsPhoneUniqueAsync(customer.Phone, customer.CustomerId))
            {
                return (false, "Another customer with this phone number already exists.");
            }

            var oldValue = $"Name: {existing.CustomerName}, Email: {existing.Email}, Phone: {existing.Phone}, Status: {existing.Status}";

            existing.CustomerName = customer.CustomerName;
            existing.Email = customer.Email;
            existing.Phone = customer.Phone;
            existing.CompanyName = customer.CompanyName;
            existing.Address = customer.Address;
            existing.City = customer.City;
            existing.State = customer.State;
            existing.Status = customer.Status;
            existing.ModifiedDate = DateTime.UtcNow;

            if (!_currentUserService.IsSalesExecutive && !string.IsNullOrEmpty(customer.OwnerId))
            {
                existing.OwnerId = customer.OwnerId;
            }

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "Customer Updated",
                entityName: "Customer",
                recordId: existing.CustomerId.ToString(),
                oldValue: oldValue,
                newValue: $"Name: {existing.CustomerName}, Email: {existing.Email}, Phone: {existing.Phone}, Status: {existing.Status}",
                result: "Success",
                details: $"Customer {existing.CustomerName} updated");

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteOrDeactivateCustomerAsync(int id)
        {
            var existing = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == id);
            if (existing == null)
            {
                return (false, "Customer not found.");
            }

            if (_currentUserService.IsSalesExecutive)
            {
                return (false, "Sales Executives are not authorized to delete or deactivate customers.");
            }

            // Soft-deactivate to preserve foreign keys and audit history
            existing.Status = "Inactive";
            existing.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "Customer Deactivated",
                entityName: "Customer",
                recordId: existing.CustomerId.ToString(),
                oldValue: "Status: Active",
                newValue: "Status: Inactive",
                result: "Success",
                details: $"Customer {existing.CustomerName} deactivated");

            return (true, null);
        }
    }
}
