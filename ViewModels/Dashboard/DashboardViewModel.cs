using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.ViewModels.Dashboard
{
    public class DashboardViewModel
    {
        // KPI Summary Cards
        public int TotalCustomers { get; set; }
        public int TotalLeads { get; set; }
        public int OpenLeads { get; set; }
        public int TotalOpportunities { get; set; }
        public int OpenOpportunities { get; set; }
        public int WonOpportunities { get; set; }
        public int LostOpportunities { get; set; }
        public decimal TotalPipelineValue { get; set; }
        public decimal WeightedPipelineValue { get; set; }
        public int PendingFollowUpsCount { get; set; }
        public int OverdueFollowUpsCount { get; set; }

        // Filter
        public string DateFilter { get; set; } = "All";

        // Chart Data: Lead Status
        public List<string> LeadStatusLabels { get; set; } = new();
        public List<int> LeadStatusCounts { get; set; } = new();

        // Chart Data: Opportunity Pipeline
        public List<string> PipelineStageLabels { get; set; } = new();
        public List<decimal> PipelineStageAmounts { get; set; } = new();
        public List<int> PipelineStageCounts { get; set; } = new();

        // Chart Data: Monthly Closed / Won Sales
        public List<string> MonthlySalesLabels { get; set; } = new();
        public List<decimal> MonthlySalesAmounts { get; set; } = new();

        // Lists
        public List<FollowUp> UpcomingFollowUps { get; set; } = new();
        public List<Activity> RecentActivities { get; set; } = new();
        public List<Opportunity> HighValueOpportunities { get; set; } = new();
    }
}
