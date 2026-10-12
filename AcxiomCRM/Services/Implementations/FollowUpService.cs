using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Interfaces;

namespace AcxiomCRM.Services.Implementations
{
    public class FollowUpService : IFollowUpService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditService _auditService;

        public FollowUpService(
            ApplicationDbContext context,
            ICurrentUserService currentUserService,
            IAuditService auditService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _auditService = auditService;
        }

        private IQueryable<FollowUp> GetScopedQuery()
        {
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.Opportunity)
                .Include(f => f.AssignedUser)
                .AsQueryable();

            if (_currentUserService.IsSalesExecutive)
            {
                query = query.Where(f => f.AssignedTo == _currentUserService.UserId);
            }

            return query;
        }

        public async Task<IEnumerable<FollowUp>> GetFollowUpsAsync(
            string? status = null,
            string? type = null,
            bool upcomingOnly = false,
            bool overdueOnly = false)
        {
            var query = GetScopedQuery();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(f => f.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(f => f.FollowUpType == type);
            }

            var now = DateTime.UtcNow;

            if (upcomingOnly)
            {
                query = query.Where(f => f.Status == "Planned" && f.FollowUpDate >= now);
            }

            if (overdueOnly)
            {
                query = query.Where(f => f.Status == "Planned" && f.FollowUpDate < now);
            }

            return await query.OrderBy(f => f.FollowUpDate).ToListAsync();
        }

        public async Task<FollowUp?> GetFollowUpByIdAsync(int id)
        {
            return await GetScopedQuery().FirstOrDefaultAsync(f => f.FollowUpId == id);
        }

        public async Task<(bool Success, string? ErrorMessage, FollowUp? FollowUp)> CreateFollowUpAsync(FollowUp followUp)
        {
            if (followUp.Status == "Planned" && followUp.FollowUpDate.Date < DateTime.Today)
            {
                return (false, "Follow-up date cannot be earlier than today.", null);
            }

            if (string.IsNullOrEmpty(followUp.AssignedTo) && _currentUserService.IsSalesExecutive)
            {
                followUp.AssignedTo = _currentUserService.UserId;
            }

            followUp.CreatedDate = DateTime.UtcNow;

            await _context.FollowUps.AddAsync(followUp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "FollowUp Scheduled",
                entityName: "FollowUp",
                recordId: followUp.FollowUpId.ToString(),
                newValue: $"Subject: {followUp.Subject}, Date: {followUp.FollowUpDate:yyyy-MM-dd HH:mm}, Status: {followUp.Status}",
                result: "Success",
                details: $"Follow-up scheduled: {followUp.Subject}");

            return (true, null, followUp);
        }

        public async Task<(bool Success, string? ErrorMessage)> CompleteFollowUpAsync(int id, string? remarks = null)
        {
            var existing = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == id);
            if (existing == null)
            {
                return (false, "Follow-up not found.");
            }

            if (_currentUserService.IsSalesExecutive && existing.AssignedTo != _currentUserService.UserId)
            {
                return (false, "You are not authorized to complete this follow-up.");
            }

            existing.Status = "Completed";
            if (!string.IsNullOrWhiteSpace(remarks))
            {
                existing.Remarks = (existing.Remarks + "\n" + $"[Completed on {DateTime.UtcNow:yyyy-MM-dd HH:mm}]: {remarks}").Trim();
            }

            // Automatically log corresponding Activity touchpoint
            var activity = new Activity
            {
                ActivityType = existing.FollowUpType,
                Subject = $"Completed: {existing.Subject}",
                Description = remarks ?? existing.Remarks,
                ActivityDate = DateTime.UtcNow,
                CustomerId = existing.CustomerId,
                LeadId = existing.LeadId,
                AssignedTo = existing.AssignedTo,
                Status = "Completed",
                CreatedDate = DateTime.UtcNow
            };
            await _context.Activities.AddAsync(activity);

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "FollowUp Completed",
                entityName: "FollowUp",
                recordId: existing.FollowUpId.ToString(),
                newValue: "Status: Completed",
                result: "Success",
                details: $"Follow-up completed: {existing.Subject}");

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> RescheduleFollowUpAsync(int id, DateTime newDate, string? remarks = null)
        {
            var existing = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == id);
            if (existing == null)
            {
                return (false, "Follow-up not found.");
            }

            if (newDate.Date < DateTime.Today)
            {
                return (false, "Rescheduled follow-up date cannot be earlier than today.");
            }

            var oldDateStr = existing.FollowUpDate.ToString("yyyy-MM-dd HH:mm");
            existing.FollowUpDate = newDate;
            existing.Status = "Planned";

            if (!string.IsNullOrWhiteSpace(remarks))
            {
                existing.Remarks = (existing.Remarks + "\n" + $"[Rescheduled on {DateTime.UtcNow:yyyy-MM-dd} from {oldDateStr}]: {remarks}").Trim();
            }

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "FollowUp Rescheduled",
                entityName: "FollowUp",
                recordId: existing.FollowUpId.ToString(),
                oldValue: $"Date: {oldDateStr}",
                newValue: $"Date: {newDate:yyyy-MM-dd HH:mm}",
                result: "Success",
                details: $"Follow-up rescheduled: {existing.Subject}");

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> CancelFollowUpAsync(int id, string? remarks = null)
        {
            var existing = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == id);
            if (existing == null)
            {
                return (false, "Follow-up not found.");
            }

            existing.Status = "Cancelled";
            if (!string.IsNullOrWhiteSpace(remarks))
            {
                existing.Remarks = (existing.Remarks + "\n" + $"[Cancelled]: {remarks}").Trim();
            }

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "FollowUp Cancelled",
                entityName: "FollowUp",
                recordId: existing.FollowUpId.ToString(),
                oldValue: "Status: Planned",
                newValue: "Status: Cancelled",
                result: "Success");

            return (true, null);
        }
    }
}
