using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.Services.Interfaces
{
    public interface ILeadService
    {
        Task<IEnumerable<Lead>> GetLeadsAsync(string? search = null, string? status = null, string? assignedTo = null);
        Task<Lead?> GetLeadByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage, Lead? Lead)> CreateLeadAsync(Lead lead);
        Task<(bool Success, string? ErrorMessage)> UpdateLeadAsync(Lead lead);
        Task<(bool Success, string? ErrorMessage)> DeleteLeadAsync(int id);
        Task<(bool Success, string? ErrorMessage, int? CustomerId)> ConvertLeadAsync(int leadId, decimal? opportunityAmount = null, string? opportunityName = null);
    }
}
