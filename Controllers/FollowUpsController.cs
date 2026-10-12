using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels.FollowUps;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class FollowUpsController : Controller
    {
        private readonly IFollowUpService _followUpService;
        private readonly ApplicationDbContext _context;

        public FollowUpsController(IFollowUpService followUpService, ApplicationDbContext context)
        {
            _followUpService = followUpService;
            _context = context;
        }

        public async Task<IActionResult> Index(string? statusFilter, string? typeFilter, bool upcomingOnly = false, bool overdueOnly = false)
        {
            ViewData["PageHeader"] = "Follow-Up Schedules";
            var followUps = await _followUpService.GetFollowUpsAsync(statusFilter, typeFilter, upcomingOnly, overdueOnly);
            var vm = new FollowUpListViewModel
            {
                FollowUps = followUps,
                StatusFilter = statusFilter,
                TypeFilter = typeFilter,
                UpcomingOnly = upcomingOnly,
                OverdueOnly = overdueOnly
            };
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null, int? opportunityId = null)
        {
            ViewData["PageHeader"] = "Schedule Follow-Up";
            await PopulateDropdownsAsync(customerId, leadId, opportunityId);

            var vm = new FollowUpCreateEditViewModel
            {
                CustomerId = customerId,
                LeadId = leadId,
                OpportunityId = opportunityId,
                RelatedType = customerId.HasValue ? "Customer" : leadId.HasValue ? "Lead" : opportunityId.HasValue ? "Opportunity" : "Customer",
                FollowUpDate = DateTime.Today.AddDays(1).AddHours(10)
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FollowUpCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model.CustomerId, model.LeadId, model.OpportunityId, model.AssignedTo);
                return View(model);
            }

            var followUp = new FollowUp
            {
                CustomerId = model.CustomerId > 0 ? model.CustomerId : null,
                LeadId = model.LeadId > 0 ? model.LeadId : null,
                OpportunityId = model.OpportunityId > 0 ? model.OpportunityId : null,
                RelatedType = model.RelatedType,
                FollowUpDate = model.FollowUpDate,
                FollowUpType = model.FollowUpType,
                Subject = model.Subject,
                Remarks = model.Remarks,
                Status = model.Status,
                AssignedTo = model.AssignedTo
            };

            var (success, errorMessage, createdFollowUp) = await _followUpService.CreateFollowUpAsync(followUp);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to create follow-up.");
                await PopulateDropdownsAsync(model.CustomerId, model.LeadId, model.OpportunityId, model.AssignedTo);
                return View(model);
            }

            TempData["SuccessMessage"] = "Follow-up successfully scheduled.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id, string? remarks)
        {
            var (success, errorMessage) = await _followUpService.CompleteFollowUpAsync(id, remarks);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Follow-up marked as Completed and logged to activities.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reschedule(int id, DateTime newDate, string? remarks)
        {
            var (success, errorMessage) = await _followUpService.RescheduleFollowUpAsync(id, newDate, remarks);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Follow-up successfully rescheduled.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? remarks)
        {
            var (success, errorMessage) = await _followUpService.CancelFollowUpAsync(id, remarks);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Follow-up cancelled.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdownsAsync(int? selectedCustId = null, int? selectedLeadId = null, int? selectedOppId = null, string? selectedUserId = null)
        {
            var customers = await _context.Customers.OrderBy(c => c.CustomerName).ToListAsync();
            ViewBag.CustomersList = new SelectList(customers, "CustomerId", "CustomerName", selectedCustId);

            var leads = await _context.Leads.OrderBy(l => l.LeadName).ToListAsync();
            ViewBag.LeadsList = new SelectList(leads, "LeadId", "LeadName", selectedLeadId);

            var opps = await _context.Opportunities.OrderBy(o => o.OpportunityName).ToListAsync();
            ViewBag.OpportunitiesList = new SelectList(opps, "OpportunityId", "OpportunityName", selectedOppId);

            var users = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            ViewBag.UsersList = new SelectList(users, "Id", "FullName", selectedUserId);
        }
    }
}
