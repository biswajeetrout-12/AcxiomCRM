using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IActivityService
    {
        Task<IEnumerable<Activity>> GetActivitiesAsync(string? activityType = null, int? customerId = null, int? leadId = null);
        Task<Activity?> GetActivityByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage, Activity? Activity)> CreateActivityAsync(Activity activity);
    }
}
