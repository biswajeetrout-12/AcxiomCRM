using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IOpportunityService
    {
        Task<IEnumerable<Opportunity>> GetOpportunitiesAsync(string? search = null, string? stage = null, string? status = null);
        Task<Opportunity?> GetOpportunityByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage, Opportunity? Opportunity)> CreateOpportunityAsync(Opportunity opportunity);
        Task<(bool Success, string? ErrorMessage)> UpdateOpportunityAsync(Opportunity opportunity);
        Task<(bool Success, string? ErrorMessage)> DeleteOpportunityAsync(int id);
        Task<decimal> GetTotalPipelineValueAsync();
    }
}
