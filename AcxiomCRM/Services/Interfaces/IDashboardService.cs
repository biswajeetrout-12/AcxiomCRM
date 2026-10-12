using AcxiomCRM.ViewModels.Dashboard;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardDataAsync(string? dateFilter = null);
    }
}
