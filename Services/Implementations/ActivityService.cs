using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Interfaces;

namespace AcxiomCRM.Services.Implementations
{
    public class ActivityService : IActivityService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditService _auditService;

        public ActivityService(
            ApplicationDbContext context,
            ICurrentUserService currentUserService,
            IAuditService auditService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _auditService = auditService;
        }

        private IQueryable<Activity> GetScopedQuery()
        {
            var query = _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.AssignedUser)
                .AsQueryable();

            if (_currentUserService.IsSalesExecutive)
            {
                query = query.Where(a => a.AssignedTo == _currentUserService.UserId);
            }

            return query;
        }

        public async Task<IEnumerable<Activity>> GetActivitiesAsync(string? activityType = null, int? customerId = null, int? leadId = null)
        {
            var query = GetScopedQuery();

            if (!string.IsNullOrWhiteSpace(activityType))
            {
                query = query.Where(a => a.ActivityType == activityType);
            }

            if (customerId.HasValue)
            {
                query = query.Where(a => a.CustomerId == customerId.Value);
            }

            if (leadId.HasValue)
            {
                query = query.Where(a => a.LeadId == leadId.Value);
            }

            return await query.OrderByDescending(a => a.ActivityDate).ToListAsync();
        }

        public async Task<Activity?> GetActivityByIdAsync(int id)
        {
            return await GetScopedQuery().FirstOrDefaultAsync(a => a.ActivityId == id);
        }

        public async Task<(bool Success, string? ErrorMessage, Activity? Activity)> CreateActivityAsync(Activity activity)
        {
            if (string.IsNullOrEmpty(activity.AssignedTo) && _currentUserService.IsSalesExecutive)
            {
                activity.AssignedTo = _currentUserService.UserId;
            }

            activity.CreatedDate = DateTime.UtcNow;

            await _context.Activities.AddAsync(activity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "Activity Logged",
                entityName: "Activity",
                recordId: activity.ActivityId.ToString(),
                newValue: $"Type: {activity.ActivityType}, Subject: {activity.Subject}",
                result: "Success",
                details: $"Activity logged: {activity.Subject}");

            return (true, null, activity);
        }
    }
}
