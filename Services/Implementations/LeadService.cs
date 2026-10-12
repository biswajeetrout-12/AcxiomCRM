using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Interfaces;

namespace AcxiomCRM.Services.Implementations
{
    public class LeadService : ILeadService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditService _auditService;

        public LeadService(
            ApplicationDbContext context,
            ICurrentUserService currentUserService,
            IAuditService auditService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _auditService = auditService;
        }

        private IQueryable<Lead> GetScopedQuery()
        {
            var query = _context.Leads
                .Include(l => l.AssignedUser)
                .Include(l => l.ConvertedCustomer)
                .AsQueryable();

            if (_currentUserService.IsSalesExecutive)
            {
                query = query.Where(l => l.AssignedTo == _currentUserService.UserId);
            }

            return query;
        }

        public async Task<IEnumerable<Lead>> GetLeadsAsync(string? search = null, string? status = null, string? assignedTo = null)
        {
            var query = GetScopedQuery();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(l => l.LeadName.Contains(search) ||
                                         l.Email.Contains(search) ||
                                         l.Phone.Contains(search) ||
                                         (l.CompanyName != null && l.CompanyName.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(l => l.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(assignedTo) && !_currentUserService.IsSalesExecutive)
            {
                query = query.Where(l => l.AssignedTo == assignedTo);
            }

            return await query.OrderByDescending(l => l.CreatedDate).ToListAsync();
        }

        public async Task<Lead?> GetLeadByIdAsync(int id)
        {
            return await GetScopedQuery()
                .Include(l => l.Opportunities)
                .Include(l => l.FollowUps)
                .Include(l => l.Activities)
                .FirstOrDefaultAsync(l => l.LeadId == id);
        }

        public async Task<(bool Success, string? ErrorMessage, Lead? Lead)> CreateLeadAsync(Lead lead)
        {
            if (lead.ExpectedValue < 0)
            {
                return (false, "Expected Value cannot be negative.", null);
            }

            // Auto-assign owner to current user if not provided and current user is Sales Executive
            if (string.IsNullOrEmpty(lead.AssignedTo) && _currentUserService.IsSalesExecutive)
            {
                lead.AssignedTo = _currentUserService.UserId;
            }

            if (string.IsNullOrWhiteSpace(lead.LeadCode))
            {
                var maxId = await _context.Leads.MaxAsync(l => (int?)l.LeadId) ?? 1000;
                lead.LeadCode = $"LEAD-{maxId + 1}";
            }

            lead.CreatedDate = DateTime.UtcNow;

            await _context.Leads.AddAsync(lead);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "Lead Created",
                entityName: "Lead",
                recordId: lead.LeadId.ToString(),
                newValue: $"Code: {lead.LeadCode}, Name: {lead.LeadName}, Status: {lead.Status}, Value: {lead.ExpectedValue}",
                result: "Success",
                details: $"Lead {lead.LeadName} created");

            return (true, null, lead);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateLeadAsync(Lead lead)
        {
            var existing = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == lead.LeadId);
            if (existing == null)
            {
                return (false, "Lead not found.");
            }

            if (_currentUserService.IsSalesExecutive && existing.AssignedTo != _currentUserService.UserId)
            {
                return (false, "You are not authorized to update this lead.");
            }

            var oldValue = $"Name: {existing.LeadName}, Status: {existing.Status}, Value: {existing.ExpectedValue}";

            existing.LeadName = lead.LeadName;
            existing.Email = lead.Email;
            existing.Phone = lead.Phone;
            existing.CompanyName = lead.CompanyName;
            existing.Source = lead.Source;
            existing.Status = lead.Status;
            existing.Priority = lead.Priority;
            existing.ExpectedValue = lead.ExpectedValue;
            existing.Notes = lead.Notes;
            existing.ModifiedDate = DateTime.UtcNow;

            if (!_currentUserService.IsSalesExecutive && !string.IsNullOrEmpty(lead.AssignedTo))
            {
                existing.AssignedTo = lead.AssignedTo;
            }

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "Lead Updated",
                entityName: "Lead",
                recordId: existing.LeadId.ToString(),
                oldValue: oldValue,
                newValue: $"Name: {existing.LeadName}, Status: {existing.Status}, Value: {existing.ExpectedValue}",
                result: "Success",
                details: $"Lead {existing.LeadName} updated");

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteLeadAsync(int id)
        {
            var existing = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == id);
            if (existing == null)
            {
                return (false, "Lead not found.");
            }

            if (_currentUserService.IsSalesExecutive)
            {
                return (false, "Sales Executives are not authorized to delete leads.");
            }

            existing.Status = "Lost";
            existing.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "Lead Deactivated / Lost",
                entityName: "Lead",
                recordId: existing.LeadId.ToString(),
                oldValue: "Status: Active",
                newValue: "Status: Lost",
                result: "Success",
                details: $"Lead {existing.LeadName} marked as Lost");

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage, int? CustomerId)> ConvertLeadAsync(
            int leadId,
            decimal? opportunityAmount = null,
            string? opportunityName = null)
        {
            var lead = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == leadId);
            if (lead == null)
            {
                return (false, "Lead not found.", null);
            }

            if (lead.Status == "Converted")
            {
                return (false, "This lead has already been converted.", lead.ConvertedCustomerId);
            }

            var isRelational = _context.Database.IsRelational();
            await using var transaction = isRelational ? await _context.Database.BeginTransactionAsync() : null;

            try
            {
                // Check if customer with same email or phone exists already
                var existingCustomer = await _context.Customers.FirstOrDefaultAsync(
                    c => c.Email.ToLower() == lead.Email.ToLower() || c.Phone == lead.Phone);

                Customer targetCustomer;

                if (existingCustomer != null)
                {
                    targetCustomer = existingCustomer;
                }
                else
                {
                    var maxCustId = await _context.Customers.MaxAsync(c => (int?)c.CustomerId) ?? 1000;
                    targetCustomer = new Customer
                    {
                        CustomerCode = $"CUST-{maxCustId + 1}",
                        CustomerName = !string.IsNullOrWhiteSpace(lead.CompanyName) ? lead.CompanyName : lead.LeadName,
                        Email = lead.Email,
                        Phone = lead.Phone,
                        CompanyName = lead.CompanyName,
                        Status = "Active",
                        OwnerId = lead.AssignedTo ?? _currentUserService.UserId,
                        CreatedBy = _currentUserService.UserName ?? "Lead Conversion",
                        CreatedDate = DateTime.UtcNow
                    };

                    await _context.Customers.AddAsync(targetCustomer);
                    await _context.SaveChangesAsync();
                }

                // Optionally create Opportunity
                Opportunity? opp = null;
                if (opportunityAmount.HasValue && opportunityAmount.Value > 0)
                {
                    opp = new Opportunity
                    {
                        OpportunityName = !string.IsNullOrWhiteSpace(opportunityName) 
                            ? opportunityName 
                            : $"{targetCustomer.CustomerName} - New Opportunity",
                        CustomerId = targetCustomer.CustomerId,
                        LeadId = lead.LeadId,
                        Amount = opportunityAmount.Value,
                        Stage = "Qualification",
                        Probability = 25,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
                        Status = "Open",
                        AssignedTo = lead.AssignedTo ?? _currentUserService.UserId,
                        Notes = $"Generated from converted lead {lead.LeadCode}: {lead.Notes}",
                        CreatedDate = DateTime.UtcNow
                    };

                    await _context.Opportunities.AddAsync(opp);
                    await _context.SaveChangesAsync();
                }

                // Update Lead to Converted
                lead.Status = "Converted";
                lead.ConvertedCustomerId = targetCustomer.CustomerId;
                lead.ModifiedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }

                // Audit logging
                if (existingCustomer == null)
                {
                    await _auditService.LogAsync(
                        action: "Customer Created From Lead",
                        entityName: "Customer",
                        recordId: targetCustomer.CustomerId.ToString(),
                        newValue: $"Customer: {targetCustomer.CustomerName}, Converted from Lead: {lead.LeadCode}",
                        result: "Success");
                }

                if (opp != null)
                {
                    await _auditService.LogAsync(
                        action: "Opportunity Created From Lead",
                        entityName: "Opportunity",
                        recordId: opp.OpportunityId.ToString(),
                        newValue: $"Opp: {opp.OpportunityName}, Amount: {opp.Amount}",
                        result: "Success");
                }

                await _auditService.LogAsync(
                    action: "Lead Converted",
                    entityName: "Lead",
                    recordId: lead.LeadId.ToString(),
                    newValue: $"Converted to CustomerId: {targetCustomer.CustomerId}",
                    result: "Success",
                    details: $"Lead {lead.LeadName} converted to customer {targetCustomer.CustomerName}");

                return (true, null, targetCustomer.CustomerId);
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }
                return (false, $"Lead conversion failed: {ex.Message}", null);
            }
        }
    }
}
