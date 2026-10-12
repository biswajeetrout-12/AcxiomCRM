using AcxiomCRM.ViewModels.Reports;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IReportService
    {
        Task<MasterReportViewModel> GenerateReportAsync(ReportFilterViewModel filter);
        Task<byte[]> ExportReportCsvAsync(ReportFilterViewModel filter);
    }
}

