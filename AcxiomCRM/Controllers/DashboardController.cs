using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Services.Interfaces;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index(string? dateFilter = "All")
        {
            ViewData["PageHeader"] = "Executive Sales & CRM Dashboard";
            var model = await _dashboardService.GetDashboardDataAsync(dateFilter);
            return View(model);
        }
    }
}
