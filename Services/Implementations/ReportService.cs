using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels.Reports;

namespace AcxiomCRM.Services.Implementations
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public ReportService(ApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<MasterReportViewModel> GenerateReportAsync(ReportFilterViewModel filter)
        {
            var vm = new MasterReportViewModel { Filter = filter };

            // Apply Role Scoping
            var userId = _currentUserService.UserId;
            var isSales = _currentUserService.IsSalesExecutive;

            var oppQuery = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedUser)
                .AsQueryable();

            var leadQuery = _context.Leads
                .Include(l => l.AssignedUser)
                .Include(l => l.ConvertedCustomer)
                .AsQueryable();

            var custQuery = _context.Customers
                .Include(c => c.Owner)
                .AsQueryable();

            var followUpQuery = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.AssignedUser)
                .AsQueryable();

            var activityQuery = _context.Activities
                .Include(a => a.AssignedUser)
                .AsQueryable();

            var auditQuery = _context.AuditLogs
                .Include(a => a.User)
                .AsQueryable();

            if (isSales)
            {
                oppQuery = oppQuery.Where(o => o.AssignedTo == userId);
                leadQuery = leadQuery.Where(l => l.AssignedTo == userId);
                custQuery = custQuery.Where(c => c.OwnerId == userId);
                followUpQuery = followUpQuery.Where(f => f.AssignedTo == userId);
                activityQuery = activityQuery.Where(a => a.AssignedTo == userId);
            }

            // Apply Date Filters if provided
            if (filter.StartDate.HasValue)
            {
                oppQuery = oppQuery.Where(o => o.CreatedDate >= filter.StartDate.Value);
                leadQuery = leadQuery.Where(l => l.CreatedDate >= filter.StartDate.Value);
                custQuery = custQuery.Where(c => c.CreatedDate >= filter.StartDate.Value);
                followUpQuery = followUpQuery.Where(f => f.CreatedDate >= filter.StartDate.Value);
                activityQuery = activityQuery.Where(a => a.ActivityDate >= filter.StartDate.Value);
                auditQuery = auditQuery.Where(a => a.CreatedDate >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                var endInclusive = filter.EndDate.Value.Date.AddDays(1);
                oppQuery = oppQuery.Where(o => o.CreatedDate < endInclusive);
                leadQuery = leadQuery.Where(l => l.CreatedDate < endInclusive);
                custQuery = custQuery.Where(c => c.CreatedDate < endInclusive);
                followUpQuery = followUpQuery.Where(f => f.CreatedDate < endInclusive);
                activityQuery = activityQuery.Where(a => a.ActivityDate < endInclusive);
                auditQuery = auditQuery.Where(a => a.CreatedDate < endInclusive);
            }

            // Pipeline Report calculation
            var oppList = await oppQuery.ToListAsync();
            vm.PipelineItems = oppList
                .GroupBy(o => new { o.Stage, OwnerName = o.AssignedUser?.FullName ?? "Unassigned" })
                .Select(g => new PipelineReportItem
                {
                    Stage = g.Key.Stage,
                    OwnerName = g.Key.OwnerName,
                    OpportunityCount = g.Count(),
                    TotalAmount = g.Sum(x => x.Amount),
                    WeightedAmount = g.Sum(x => (x.Amount * x.Probability) / 100m)
                })
                .OrderBy(x => x.Stage)
                .ToList();

            // Conversion Report calculation
            var leadList = await leadQuery.ToListAsync();
            vm.ConversionItems = leadList
                .GroupBy(l => l.Source)
                .Select(g => new ConversionReportItem
                {
                    Source = g.Key,
                    TotalLeads = g.Count(),
                    ConvertedLeads = g.Count(x => x.Status == "Converted"),
                    ConvertedValue = g.Where(x => x.Status == "Converted").Sum(x => x.ExpectedValue)
                })
                .OrderByDescending(x => x.TotalLeads)
                .ToList();

            // User Activity Report calculation
            var activities = await activityQuery.ToListAsync();
            vm.UserActivityItems = activities
                .GroupBy(a => a.AssignedUser?.FullName ?? "Unknown User")
                .Select(g => new UserActivityReportItem
                {
                    UserName = g.Key,
                    Role = "Sales Executive",
                    CallsCount = g.Count(x => x.ActivityType == "Call"),
                    MeetingsCount = g.Count(x => x.ActivityType == "Meeting"),
                    EmailsCount = g.Count(x => x.ActivityType == "Email"),
                    TasksCount = g.Count(x => x.ActivityType == "Task")
                })
                .OrderByDescending(x => x.TotalActivities)
                .ToList();

            // Populate Raw Collections for detailed reports
            vm.Opportunities = oppList;
            vm.Leads = leadList;
            vm.Customers = await custQuery.OrderByDescending(c => c.CreatedDate).ToListAsync();
            vm.FollowUps = await followUpQuery.OrderByDescending(f => f.FollowUpDate).ToListAsync();

            if (_currentUserService.IsAdmin)
            {
                vm.AuditLogs = await auditQuery.OrderByDescending(a => a.CreatedDate).Take(100).ToListAsync();
            }

            return vm;
        }

        public async Task<byte[]> ExportReportCsvAsync(ReportFilterViewModel filter)
        {
            var model = await GenerateReportAsync(filter);
            var sb = new System.Text.StringBuilder();

            // Helper to sanitize formula injection (CWE-1236)
            static string SanitizeCsvCell(string? input)
            {
                if (string.IsNullOrEmpty(input))
                    return "";

                // If cell begins with =, +, -, @, \t, or \r, prefix with single quote to prevent spreadsheet code execution
                var trimmed = input.TrimStart();
                if (trimmed.Length > 0 && (trimmed[0] == '=' || trimmed[0] == '+' || trimmed[0] == '-' || trimmed[0] == '@' || trimmed[0] == '\t' || trimmed[0] == '\r'))
                {
                    input = "'" + input;
                }

                if (input.Contains("\"") || input.Contains(",") || input.Contains("\n") || input.Contains("\r"))
                {
                    return $"\"{input.Replace("\"", "\"\"")}\"";
                }

                return input;
            }

            if (filter.ReportType == "Conversion")
            {
                sb.AppendLine("Lead Source,Total Leads,Converted Leads,Converted Value");
                foreach (var item in model.ConversionItems)
                {
                    sb.AppendLine($"{SanitizeCsvCell(item.Source)},{item.TotalLeads},{item.ConvertedLeads},{item.ConvertedValue:F2}");
                }
            }
            else if (filter.ReportType == "Activity")
            {
                sb.AppendLine("User Name,Role,Calls,Meetings,Emails,Tasks,Total Activities");
                foreach (var item in model.UserActivityItems)
                {
                    sb.AppendLine($"{SanitizeCsvCell(item.UserName)},{SanitizeCsvCell(item.Role)},{item.CallsCount},{item.MeetingsCount},{item.EmailsCount},{item.TasksCount},{item.TotalActivities}");
                }
            }
            else if (filter.ReportType == "Customers")
            {
                sb.AppendLine("Customer Code,Customer Name,Email,Phone,Company,Status,Owner,Created Date");
                foreach (var c in model.Customers)
                {
                    sb.AppendLine($"{SanitizeCsvCell(c.CustomerCode)},{SanitizeCsvCell(c.CustomerName)},{SanitizeCsvCell(c.Email)},{SanitizeCsvCell(c.Phone)},{SanitizeCsvCell(c.CompanyName)},{SanitizeCsvCell(c.Status)},{SanitizeCsvCell(c.Owner?.FullName)},{c.CreatedDate:yyyy-MM-dd}");
                }
            }
            else if (filter.ReportType == "FollowUps")
            {
                sb.AppendLine("FollowUp Date,Type,Subject,Status,Assigned To,Customer / Lead");
                foreach (var f in model.FollowUps)
                {
                    var related = f.Customer?.CustomerName ?? f.Lead?.LeadName ?? "N/A";
                    sb.AppendLine($"{f.FollowUpDate:yyyy-MM-dd HH:mm},{SanitizeCsvCell(f.FollowUpType)},{SanitizeCsvCell(f.Subject)},{SanitizeCsvCell(f.Status)},{SanitizeCsvCell(f.AssignedUser?.FullName)},{SanitizeCsvCell(related)}");
                }
            }
            else // Default Pipeline
            {
                sb.AppendLine("Pipeline Stage,Account Owner,Opportunity Count,Face Amount,Weighted Amount");
                foreach (var p in model.PipelineItems)
                {
                    sb.AppendLine($"{SanitizeCsvCell(p.Stage)},{SanitizeCsvCell(p.OwnerName)},{p.OpportunityCount},{p.TotalAmount:F2},{p.WeightedAmount:F2}");
                }
            }

            return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}
