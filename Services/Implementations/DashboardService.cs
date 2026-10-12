using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels.Dashboard;

namespace AcxiomCRM.Services.Implementations
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public DashboardService(ApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync(string? dateFilter = null)
        {
            var vm = new DashboardViewModel
            {
                DateFilter = string.IsNullOrWhiteSpace(dateFilter) ? "All" : dateFilter
            };

            // Base queries with role scoping
            var customerQuery = _context.Customers.AsQueryable();
            var leadQuery = _context.Leads.AsQueryable();
            var oppQuery = _context.Opportunities.AsQueryable();
            var followUpQuery = _context.FollowUps.AsQueryable();
            var activityQuery = _context.Activities.AsQueryable();

            if (_currentUserService.IsSalesExecutive)
            {
                var userId = _currentUserService.UserId;
                customerQuery = customerQuery.Where(c => c.OwnerId == userId);
                leadQuery = leadQuery.Where(l => l.AssignedTo == userId);
                oppQuery = oppQuery.Where(o => o.AssignedTo == userId);
                followUpQuery = followUpQuery.Where(f => f.AssignedTo == userId);
                activityQuery = activityQuery.Where(a => a.AssignedTo == userId);
            }

            // Apply Date Filter if specified
            DateTime? startDate = null;
            var now = DateTime.UtcNow;

            switch (dateFilter?.ToLower())
            {
                case "today":
                    startDate = DateTime.Today;
                    break;
                case "thisweek":
                    startDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                    break;
                case "thismonth":
                    startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    break;
            }

            if (startDate.HasValue)
            {
                customerQuery = customerQuery.Where(c => c.CreatedDate >= startDate.Value);
                leadQuery = leadQuery.Where(l => l.CreatedDate >= startDate.Value);
                oppQuery = oppQuery.Where(o => o.CreatedDate >= startDate.Value);
            }

            // Compute KPI Cards
            vm.TotalCustomers = await customerQuery.CountAsync();
            vm.TotalLeads = await leadQuery.CountAsync();
            vm.OpenLeads = await leadQuery.Where(l => l.Status != "Converted" && l.Status != "Lost").CountAsync();
            
            vm.TotalOpportunities = await oppQuery.CountAsync();
            vm.OpenOpportunities = await oppQuery.Where(o => o.Status == "Open").CountAsync();
            vm.WonOpportunities = await oppQuery.Where(o => o.Status == "Won").CountAsync();
            vm.LostOpportunities = await oppQuery.Where(o => o.Status == "Lost").CountAsync();

            var openOpps = await oppQuery.Where(o => o.Status == "Open").ToListAsync();
            vm.TotalPipelineValue = openOpps.Sum(o => o.Amount);
            vm.WeightedPipelineValue = openOpps.Sum(o => (o.Amount * o.Probability) / 100m);

            vm.PendingFollowUpsCount = await followUpQuery.Where(f => f.Status == "Planned" && f.FollowUpDate >= now).CountAsync();
            vm.OverdueFollowUpsCount = await followUpQuery.Where(f => f.Status == "Planned" && f.FollowUpDate < now).CountAsync();

            // Chart 1: Lead Status
            var leadStatuses = new[] { "New", "Contacted", "Qualified", "Converted", "Lost" };
            var leadGroup = await leadQuery.GroupBy(l => l.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Status, g => g.Count);

            foreach (var status in leadStatuses)
            {
                vm.LeadStatusLabels.Add(status);
                vm.LeadStatusCounts.Add(leadGroup.TryGetValue(status, out var count) ? count : 0);
            }

            // Chart 2: Opportunity Pipeline by Stage
            var stages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
            var stageGroup = await oppQuery.GroupBy(o => o.Stage)
                .Select(g => new { Stage = g.Key, Count = g.Count(), Total = g.Sum(x => x.Amount) })
                .ToDictionaryAsync(g => g.Stage, g => new { g.Count, g.Total });

            foreach (var stg in stages)
            {
                vm.PipelineStageLabels.Add(stg);
                if (stageGroup.TryGetValue(stg, out var val))
                {
                    vm.PipelineStageCounts.Add(val.Count);
                    vm.PipelineStageAmounts.Add(val.Total);
                }
                else
                {
                    vm.PipelineStageCounts.Add(0);
                    vm.PipelineStageAmounts.Add(0m);
                }
            }

            // Chart 3: Monthly Won Sales (Past 6 months)
            for (int i = 5; i >= 0; i--)
            {
                var targetMonth = DateTime.Today.AddMonths(-i);
                var monthStart = new DateTime(targetMonth.Year, targetMonth.Month, 1);
                var monthEnd = monthStart.AddMonths(1);
                var monthLabel = targetMonth.ToString("MMM yyyy");

                var monthWonTotal = await oppQuery
                    .Where(o => o.Status == "Won" && o.CreatedDate >= monthStart && o.CreatedDate < monthEnd)
                    .SumAsync(o => (decimal?)o.Amount) ?? 0m;

                vm.MonthlySalesLabels.Add(monthLabel);
                vm.MonthlySalesAmounts.Add(monthWonTotal);
            }

            // Lists
            vm.UpcomingFollowUps = await followUpQuery
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Where(f => f.Status == "Planned")
                .OrderBy(f => f.FollowUpDate)
                .Take(5)
                .ToListAsync();

            vm.RecentActivities = await activityQuery
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.AssignedUser)
                .OrderByDescending(a => a.ActivityDate)
                .Take(5)
                .ToListAsync();

            vm.HighValueOpportunities = await oppQuery
                .Include(o => o.Customer)
                .Where(o => o.Status == "Open")
                .OrderByDescending(o => o.Amount)
                .Take(5)
                .ToListAsync();

            return vm;
        }
    }
}
