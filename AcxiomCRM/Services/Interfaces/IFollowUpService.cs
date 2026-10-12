using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IFollowUpService
    {
        Task<IEnumerable<FollowUp>> GetFollowUpsAsync(string? status = null, string? type = null, bool upcomingOnly = false, bool overdueOnly = false);
        Task<FollowUp?> GetFollowUpByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage, FollowUp? FollowUp)> CreateFollowUpAsync(FollowUp followUp);
        Task<(bool Success, string? ErrorMessage)> CompleteFollowUpAsync(int id, string? remarks = null);
        Task<(bool Success, string? ErrorMessage)> RescheduleFollowUpAsync(int id, DateTime newDate, string? remarks = null);
        Task<(bool Success, string? ErrorMessage)> CancelFollowUpAsync(int id, string? remarks = null);
    }
}
