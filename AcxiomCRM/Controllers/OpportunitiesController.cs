using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels.Opportunities;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class OpportunitiesController : Controller
    {
        private readonly IOpportunityService _opportunityService;
        private readonly ApplicationDbContext _context;

        public OpportunitiesController(IOpportunityService opportunityService, ApplicationDbContext context)
        {
            _opportunityService = opportunityService;
            _context = context;
        }

        public async Task<IActionResult> Index(string? searchTerm, string? stageFilter, string? statusFilter)
        {
            ViewData["PageHeader"] = "Opportunity Pipeline";
            var opportunities = await _opportunityService.GetOpportunitiesAsync(searchTerm, stageFilter, statusFilter);
            var totalPipeline = await _opportunityService.GetTotalPipelineValueAsync();

            var vm = new OpportunityListViewModel
            {
                Opportunities = opportunities,
                SearchTerm = searchTerm,
                StageFilter = stageFilter,
                StatusFilter = statusFilter,
                TotalPipelineValue = totalPipeline,
                WeightedPipelineValue = opportunities.Where(o => o.Status == "Open").Sum(o => o.WeightedValue)
            };
            return View(vm);
        }

        public async Task<IActionResult> Details(int id)
        {
            ViewData["PageHeader"] = "Opportunity Details";
            var opp = await _opportunityService.GetOpportunityByIdAsync(id);
            if (opp == null)
            {
                return NotFound();
            }

            var vm = new OpportunityDetailsViewModel
            {
                Opportunity = opp,
                FollowUps = opp.FollowUps.OrderBy(f => f.FollowUpDate).ToList()
            };
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId = null)
        {
            ViewData["PageHeader"] = "New Opportunity";
            await PopulateDropdownsAsync(customerId);

            var vm = new OpportunityCreateEditViewModel
            {
                CustomerId = customerId ?? 0,
                ExpectedCloseDate = DateTime.Today.AddDays(30)
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OpportunityCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model.CustomerId, model.LeadId, model.AssignedTo);
                return View(model);
            }

            var opp = new Opportunity
            {
                OpportunityName = model.OpportunityName,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId > 0 ? model.LeadId : null,
                Amount = model.Amount,
                Stage = model.Stage,
                Probability = model.Probability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                Status = model.Status,
                AssignedTo = model.AssignedTo,
                Notes = model.Notes
            };

            var (success, errorMessage, createdOpp) = await _opportunityService.CreateOpportunityAsync(opp);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to create opportunity.");
                await PopulateDropdownsAsync(model.CustomerId, model.LeadId, model.AssignedTo);
                return View(model);
            }

            TempData["SuccessMessage"] = $"Opportunity '{opp.OpportunityName}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["PageHeader"] = "Edit Opportunity";
            var opp = await _opportunityService.GetOpportunityByIdAsync(id);
            if (opp == null)
            {
                return NotFound();
            }

            await PopulateDropdownsAsync(opp.CustomerId, opp.LeadId, opp.AssignedTo);

            var vm = new OpportunityCreateEditViewModel
            {
                OpportunityId = opp.OpportunityId,
                OpportunityName = opp.OpportunityName,
                CustomerId = opp.CustomerId,
                LeadId = opp.LeadId,
                Amount = opp.Amount,
                Stage = opp.Stage,
                Probability = opp.Probability,
                ExpectedCloseDate = opp.ExpectedCloseDate,
                Status = opp.Status,
                AssignedTo = opp.AssignedTo,
                Notes = opp.Notes
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OpportunityCreateEditViewModel model)
        {
            if (id != model.OpportunityId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model.CustomerId, model.LeadId, model.AssignedTo);
                return View(model);
            }

            var opp = new Opportunity
            {
                OpportunityId = model.OpportunityId,
                OpportunityName = model.OpportunityName,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId > 0 ? model.LeadId : null,
                Amount = model.Amount,
                Stage = model.Stage,
                Probability = model.Probability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                Status = model.Status,
                AssignedTo = model.AssignedTo,
                Notes = model.Notes
            };

            var (success, errorMessage) = await _opportunityService.UpdateOpportunityAsync(opp);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to update opportunity.");
                await PopulateDropdownsAsync(model.CustomerId, model.LeadId, model.AssignedTo);
                return View(model);
            }

            TempData["SuccessMessage"] = $"Opportunity '{opp.OpportunityName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var (success, errorMessage) = await _opportunityService.DeleteOpportunityAsync(id);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Opportunity marked as Abandoned.";
            }

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
