using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels.Reports;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        public async Task<IActionResult> Index(ReportFilterViewModel filter)
        {
            ViewData["PageHeader"] = "Business Intelligence & CRM Reports";
            var model = await _reportService.GenerateReportAsync(filter);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv(ReportFilterViewModel filter)
        {
            var bytes = await _reportService.ExportReportCsvAsync(filter);
            var reportName = string.IsNullOrWhiteSpace(filter.ReportType) ? "Pipeline" : filter.ReportType;
            var fileName = $"AcxiomCRM_{reportName}_Report_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv";
            return File(bytes, "text/csv", fileName);
        }
    }
}
