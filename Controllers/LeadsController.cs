using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels.Leads;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class LeadsController : Controller
    {
        private readonly ILeadService _leadService;
        private readonly ApplicationDbContext _context;

        public LeadsController(ILeadService leadService, ApplicationDbContext context)
        {
            _leadService = leadService;
            _context = context;
        }

        public async Task<IActionResult> Index(string? searchTerm, string? statusFilter, string? assignedToFilter)
        {
            ViewData["PageHeader"] = "Lead Management";
            var leads = await _leadService.GetLeadsAsync(searchTerm, statusFilter, assignedToFilter);

            var users = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            ViewBag.UsersList = new SelectList(users, "Id", "FullName", assignedToFilter);

            var vm = new LeadListViewModel
            {
                Leads = leads,
                SearchTerm = searchTerm,
                StatusFilter = statusFilter,
                AssignedToFilter = assignedToFilter
            };
            return View(vm);
        }

        public async Task<IActionResult> Details(int id)
        {
            ViewData["PageHeader"] = "Lead Details";
            var lead = await _leadService.GetLeadByIdAsync(id);
            if (lead == null)
            {
                return NotFound();
            }

            var vm = new LeadDetailsViewModel
            {
                Lead = lead,
                Opportunities = lead.Opportunities.OrderByDescending(o => o.CreatedDate).ToList(),
                FollowUps = lead.FollowUps.OrderBy(f => f.FollowUpDate).ToList(),
                Activities = lead.Activities.OrderByDescending(a => a.ActivityDate).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewData["PageHeader"] = "New Prospective Lead";
            await PopulateUsersDropdownAsync();
            return View(new LeadCreateEditViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeadCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateUsersDropdownAsync();
                return View(model);
            }

            var lead = new Lead
            {
                LeadName = model.LeadName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName,
                Source = model.Source,
                Status = model.Status,
                Priority = model.Priority,
                ExpectedValue = model.ExpectedValue,
                Notes = model.Notes,
                AssignedTo = model.AssignedTo
            };

            var (success, errorMessage, createdLead) = await _leadService.CreateLeadAsync(lead);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to create lead.");
                await PopulateUsersDropdownAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' captured successfully ({createdLead?.LeadCode}).";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["PageHeader"] = "Edit Lead";
            var lead = await _leadService.GetLeadByIdAsync(id);
            if (lead == null)
            {
                return NotFound();
            }

            await PopulateUsersDropdownAsync(lead.AssignedTo);

            var model = new LeadCreateEditViewModel
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Source = lead.Source,
                Status = lead.Status,
                Priority = lead.Priority,
                ExpectedValue = lead.ExpectedValue,
                Notes = lead.Notes,
                AssignedTo = lead.AssignedTo
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LeadCreateEditViewModel model)
        {
            if (id != model.LeadId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                await PopulateUsersDropdownAsync(model.AssignedTo);
                return View(model);
            }

            var lead = new Lead
            {
                LeadId = model.LeadId,
                LeadName = model.LeadName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName,
                Source = model.Source,
                Status = model.Status,
                Priority = model.Priority,
                ExpectedValue = model.ExpectedValue,
                Notes = model.Notes,
                AssignedTo = model.AssignedTo
            };

            var (success, errorMessage) = await _leadService.UpdateLeadAsync(lead);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to update lead.");
                await PopulateUsersDropdownAsync(model.AssignedTo);
                return View(model);
            }

            TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Convert(int id)
        {
            ViewData["PageHeader"] = "Convert Lead to Customer";
            var lead = await _leadService.GetLeadByIdAsync(id);
            if (lead == null)
            {
                return NotFound();
            }

            if (lead.Status == "Converted")
            {
                TempData["WarningMessage"] = "This lead is already converted.";
                return RedirectToAction(nameof(Details), new { id = lead.LeadId });
            }

            var vm = new LeadConvertViewModel
            {
                LeadId = lead.LeadId,
                LeadName = lead.LeadName,
                CompanyName = lead.CompanyName,
                Email = lead.Email,
                Phone = lead.Phone,
                CreateOpportunity = true,
                OpportunityName = $"{(!string.IsNullOrWhiteSpace(lead.CompanyName) ? lead.CompanyName : lead.LeadName)} - Initial Contract",
                OpportunityAmount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 10000m
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Convert(LeadConvertViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            decimal? oppAmount = model.CreateOpportunity ? model.OpportunityAmount : null;
            string? oppName = model.CreateOpportunity ? model.OpportunityName : null;

            var (success, errorMessage, customerId) = await _leadService.ConvertLeadAsync(
                model.LeadId, oppAmount, oppName);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to convert lead.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Lead successfully converted to Customer & Opportunity!";
            return RedirectToAction("Details", "Customers", new { id = customerId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var (success, errorMessage) = await _leadService.DeleteLeadAsync(id);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Lead marked as Lost.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateUsersDropdownAsync(string? selectedId = null)
        {
            var users = await _context.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .Select(u => new { u.Id, u.FullName, u.Email })
                .ToListAsync();

            ViewBag.UsersList = new SelectList(users, "Id", "FullName", selectedId);
        }
    }
}
