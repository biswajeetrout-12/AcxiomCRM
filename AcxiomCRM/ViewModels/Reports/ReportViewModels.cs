using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.ViewModels.Reports
{
    public class ReportFilterViewModel
    {
        public string ReportType { get; set; } = "Pipeline";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? AssignedTo { get; set; }
        public string? Status { get; set; }
    }

    public class PipelineReportItem
    {
        public string Stage { get; set; } = string.Empty;
        public int OpportunityCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal WeightedAmount { get; set; }
        public string OwnerName { get; set; } = string.Empty;
    }

    public class ConversionReportItem
    {
        public string Source { get; set; } = string.Empty;
        public int TotalLeads { get; set; }
        public int ConvertedLeads { get; set; }
        public decimal ConversionRate => TotalLeads > 0 ? (decimal)ConvertedLeads / TotalLeads * 100m : 0m;
        public decimal ConvertedValue { get; set; }
    }

    public class UserActivityReportItem
    {
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int CallsCount { get; set; }
        public int MeetingsCount { get; set; }
        public int EmailsCount { get; set; }
        public int TasksCount { get; set; }
        public int TotalActivities => CallsCount + MeetingsCount + EmailsCount + TasksCount;
    }

    public class MasterReportViewModel
    {
        public ReportFilterViewModel Filter { get; set; } = new();
        public List<PipelineReportItem> PipelineItems { get; set; } = new();
        public List<ConversionReportItem> ConversionItems { get; set; } = new();
        public List<UserActivityReportItem> UserActivityItems { get; set; } = new();
        public List<Customer> Customers { get; set; } = new();
        public List<Lead> Leads { get; set; } = new();
        public List<Opportunity> Opportunities { get; set; } = new();
        public List<FollowUp> FollowUps { get; set; } = new();
        public List<AuditLog> AuditLogs { get; set; } = new();
    }
}
