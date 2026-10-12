using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Interfaces;

namespace AcxiomCRM.Services.Implementations
{
    public class OpportunityService : IOpportunityService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditService _auditService;

        public OpportunityService(
            ApplicationDbContext context,
            ICurrentUserService currentUserService,
            IAuditService auditService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _auditService = auditService;
        }

        private IQueryable<Opportunity> GetScopedQuery()
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedUser)
                .AsQueryable();

            if (_currentUserService.IsSalesExecutive)
            {
                query = query.Where(o => o.AssignedTo == _currentUserService.UserId);
            }

            return query;
        }

        public async Task<IEnumerable<Opportunity>> GetOpportunitiesAsync(string? search = null, string? stage = null, string? status = null)
        {
            var query = GetScopedQuery();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(o => o.OpportunityName.Contains(search) ||
                                         o.Customer.CustomerName.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(stage))
            {
                query = query.Where(o => o.Stage == stage);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status == status);
            }

            return await query.OrderByDescending(o => o.CreatedDate).ToListAsync();
        }

        public async Task<Opportunity?> GetOpportunityByIdAsync(int id)
        {
            return await GetScopedQuery()
                .Include(o => o.FollowUps)
                .FirstOrDefaultAsync(o => o.OpportunityId == id);
        }

        public async Task<(bool Success, string? ErrorMessage, Opportunity? Opportunity)> CreateOpportunityAsync(Opportunity opportunity)
        {
            // Business validations
            if (opportunity.Amount <= 0)
            {
                return (false, "Opportunity Amount must be greater than 0.", null);
            }

            if (opportunity.Probability < 0 || opportunity.Probability > 100)
            {
                return (false, "Probability must be between 0 and 100.", null);
            }

            if (opportunity.Status == "Open" && opportunity.ExpectedCloseDate.Date < DateTime.Today)
            {
                return (false, "Expected Close Date cannot be in the past for an active Opportunity.", null);
            }

            // Sync Stage with Status
            if (opportunity.Stage == "Won")
            {
                opportunity.Status = "Won";
                opportunity.Probability = 100;
            }
            else if (opportunity.Stage == "Lost")
            {
                opportunity.Status = "Lost";
                opportunity.Probability = 0;
            }

            if (string.IsNullOrEmpty(opportunity.AssignedTo) && _currentUserService.IsSalesExecutive)
            {
                opportunity.AssignedTo = _currentUserService.UserId;
            }

            opportunity.CreatedDate = DateTime.UtcNow;

            await _context.Opportunities.AddAsync(opportunity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "Opportunity Created",
                entityName: "Opportunity",
                recordId: opportunity.OpportunityId.ToString(),
                newValue: $"Name: {opportunity.OpportunityName}, Amount: {opportunity.Amount}, Stage: {opportunity.Stage}",
                result: "Success",
                details: $"Opportunity {opportunity.OpportunityName} created");

            return (true, null, opportunity);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateOpportunityAsync(Opportunity opportunity)
        {
            var existing = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == opportunity.OpportunityId);
            if (existing == null)
            {
                return (false, "Opportunity not found.");
            }

            if (_currentUserService.IsSalesExecutive && existing.AssignedTo != _currentUserService.UserId)
            {
                return (false, "You are not authorized to update this opportunity.");
            }

            if (opportunity.Amount <= 0)
            {
                return (false, "Opportunity Amount must be greater than 0.");
            }

            if (opportunity.Probability < 0 || opportunity.Probability > 100)
            {
                return (false, "Probability must be between 0 and 100.");
            }

            if (opportunity.Status == "Open" && opportunity.ExpectedCloseDate.Date < DateTime.Today)
            {
                return (false, "Expected Close Date cannot be in the past for an active Opportunity.");
            }

            var oldValue = $"Name: {existing.OpportunityName}, Amount: {existing.Amount}, Stage: {existing.Stage}, Status: {existing.Status}";

            existing.OpportunityName = opportunity.OpportunityName;
            existing.CustomerId = opportunity.CustomerId;
            existing.LeadId = opportunity.LeadId;
            existing.Amount = opportunity.Amount;
            existing.Stage = opportunity.Stage;
            existing.Probability = opportunity.Probability;
            existing.ExpectedCloseDate = opportunity.ExpectedCloseDate;
            existing.Status = opportunity.Status;
            existing.Notes = opportunity.Notes;
            existing.ModifiedDate = DateTime.UtcNow;

            // Sync Stage with Status
            if (existing.Stage == "Won")
            {
                existing.Status = "Won";
                existing.Probability = 100;
            }
            else if (existing.Stage == "Lost")
            {
                existing.Status = "Lost";
                existing.Probability = 0;
            }

            if (!_currentUserService.IsSalesExecutive && !string.IsNullOrEmpty(opportunity.AssignedTo))
            {
                existing.AssignedTo = opportunity.AssignedTo;
            }

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "Opportunity Updated",
                entityName: "Opportunity",
                recordId: existing.OpportunityId.ToString(),
                oldValue: oldValue,
                newValue: $"Name: {existing.OpportunityName}, Amount: {existing.Amount}, Stage: {existing.Stage}, Status: {existing.Status}",
                result: "Success",
                details: $"Opportunity {existing.OpportunityName} updated");

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteOpportunityAsync(int id)
        {
            var existing = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == id);
            if (existing == null)
            {
                return (false, "Opportunity not found.");
            }

            if (_currentUserService.IsSalesExecutive)
            {
                return (false, "Sales Executives are not authorized to delete opportunities.");
            }

            existing.Status = "Abandoned";
            existing.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "Opportunity Abandoned",
                entityName: "Opportunity",
                recordId: existing.OpportunityId.ToString(),
                oldValue: "Status: Open",
                newValue: "Status: Abandoned",
                result: "Success",
                details: $"Opportunity {existing.OpportunityName} marked Abandoned");

            return (true, null);
        }

        public async Task<decimal> GetTotalPipelineValueAsync()
        {
            return await GetScopedQuery()
                .Where(o => o.Status == "Open")
                .SumAsync(o => (decimal?)o.Amount) ?? 0m;
        }
    }
}
