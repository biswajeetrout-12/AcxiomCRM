using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels.Activities;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class ActivitiesController : Controller
    {
        private readonly IActivityService _activityService;
        private readonly ApplicationDbContext _context;

        public ActivitiesController(IActivityService activityService, ApplicationDbContext context)
        {
            _activityService = activityService;
            _context = context;
        }

        public async Task<IActionResult> Index(string? typeFilter, string? statusFilter)
        {
            ViewData["PageHeader"] = "Activity Management";
            var activities = await _activityService.GetActivitiesAsync(typeFilter);
            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                activities = activities.Where(a => a.Status == statusFilter);
            }

            var vm = new ActivityListViewModel
            {
                Activities = activities,
                TypeFilter = typeFilter,
                StatusFilter = statusFilter
            };
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
        {
            ViewData["PageHeader"] = "Log Activity";
            await PopulateDropdownsAsync(customerId, leadId);

            var vm = new ActivityCreateViewModel
            {
                CustomerId = customerId,
                LeadId = leadId,
                ActivityDate = DateTime.Now
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ActivityCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model.CustomerId, model.LeadId, model.AssignedTo);
                return View(model);
            }

            var activity = new Activity
            {
                ActivityType = model.ActivityType,
                Subject = model.Subject,
                Description = model.Description,
                ActivityDate = model.ActivityDate,
                CustomerId = model.CustomerId > 0 ? model.CustomerId : null,
                LeadId = model.LeadId > 0 ? model.LeadId : null,
                AssignedTo = model.AssignedTo,
                Status = model.Status
            };

            var (success, errorMessage, createdActivity) = await _activityService.CreateActivityAsync(activity);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to log activity.");
                await PopulateDropdownsAsync(model.CustomerId, model.LeadId, model.AssignedTo);
                return View(model);
            }

            TempData["SuccessMessage"] = "Activity successfully logged.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdownsAsync(int? selectedCustId = null, int? selectedLeadId = null, string? selectedUserId = null)
        {
            var customers = await _context.Customers.OrderBy(c => c.CustomerName).ToListAsync();
            ViewBag.CustomersList = new SelectList(customers, "CustomerId", "CustomerName", selectedCustId);

            var leads = await _context.Leads.OrderBy(l => l.LeadName).ToListAsync();
            ViewBag.LeadsList = new SelectList(leads, "LeadId", "LeadName", selectedLeadId);

            var users = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            ViewBag.UsersList = new SelectList(users, "Id", "FullName", selectedUserId);
        }
    }
}
