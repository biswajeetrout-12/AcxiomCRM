using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.DTOs;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels.Reports;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/reports")]
    [Authorize]
    public class ReportsApiController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsApiController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("pipeline")]
        public async Task<IActionResult> GetPipelineReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var filter = new ReportFilterViewModel
            {
                ReportType = "Pipeline",
                StartDate = startDate,
                EndDate = endDate
            };

            var report = await _reportService.GenerateReportAsync(filter);
            return Ok(ApiResponse<List<PipelineReportItem>>.Ok(report.PipelineItems));
        }
    }
}
