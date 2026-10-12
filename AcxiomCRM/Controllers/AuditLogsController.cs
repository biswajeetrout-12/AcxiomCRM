using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Authorization;
using AcxiomCRM.Data;
using AcxiomCRM.ViewModels.Audit;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    public class AuditLogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string? moduleFilter,
            string? actionFilter,
            string? userFilter,
            DateTime? fromDate,
            DateTime? toDate)
        {
            ViewData["PageHeader"] = "Security & Business Audit Trail";

            var query = _context.AuditLogs
                .Include(a => a.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(moduleFilter))
            {
                query = query.Where(a => a.EntityName == moduleFilter);
            }

            if (!string.IsNullOrWhiteSpace(actionFilter))
            {
                query = query.Where(a => a.Action.Contains(actionFilter));
            }

            if (!string.IsNullOrWhiteSpace(userFilter))
            {
                query = query.Where(a => a.User != null && (a.User.FullName.Contains(userFilter) || a.User.Email!.Contains(userFilter)));
            }

            if (fromDate.HasValue)
            {
                query = query.Where(a => a.CreatedDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var toDateInclusive = toDate.Value.Date.AddDays(1);
                query = query.Where(a => a.CreatedDate < toDateInclusive);
            }

            var auditLogs = await query
                .OrderByDescending(a => a.CreatedDate)
                .Take(200)
                .ToListAsync();

            var vm = new AuditLogListViewModel
            {
                AuditLogs = auditLogs,
                ModuleFilter = moduleFilter,
                ActionFilter = actionFilter,
                UserFilter = userFilter,
                FromDate = fromDate,
                ToDate = toDate
            };

            return View(vm);
        }
    }
}
