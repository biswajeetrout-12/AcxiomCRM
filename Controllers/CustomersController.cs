using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels.Customers;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class CustomersController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly ApplicationDbContext _context;

        public CustomersController(ICustomerService customerService, ApplicationDbContext context)
        {
            _customerService = customerService;
            _context = context;
        }

        public async Task<IActionResult> Index(string? searchTerm, string? statusFilter)
        {
            ViewData["PageHeader"] = "Customer Management";
            var customers = await _customerService.GetCustomersAsync(searchTerm, statusFilter);
            var vm = new CustomerListViewModel
            {
                Customers = customers,
                SearchTerm = searchTerm,
                StatusFilter = statusFilter
            };
            return View(vm);
        }

        public async Task<IActionResult> Details(int id)
        {
            ViewData["PageHeader"] = "Customer Profile";
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            var vm = new CustomerDetailsViewModel
            {
                Customer = customer,
                Opportunities = customer.Opportunities.OrderByDescending(o => o.CreatedDate).ToList(),
                FollowUps = customer.FollowUps.OrderBy(f => f.FollowUpDate).ToList(),
                Activities = customer.Activities.OrderByDescending(a => a.ActivityDate).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewData["PageHeader"] = "New Customer";
            await PopulateUsersDropdownAsync();
            return View(new CustomerCreateEditViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateUsersDropdownAsync();
                return View(model);
            }

            var customer = new Customer
            {
                CustomerName = model.CustomerName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName,
                Address = model.Address,
                City = model.City,
                State = model.State,
                Status = model.Status,
                OwnerId = model.OwnerId
            };

            var (success, errorMessage, createdCustomer) = await _customerService.CreateCustomerAsync(customer);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to create customer.");
                await PopulateUsersDropdownAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' created successfully ({createdCustomer?.CustomerCode}).";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["PageHeader"] = "Edit Customer";
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            await PopulateUsersDropdownAsync(customer.OwnerId);

            var model = new CustomerCreateEditViewModel
            {
                CustomerId = customer.CustomerId,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.CustomerName,
                Email = customer.Email,
                Phone = customer.Phone,
                CompanyName = customer.CompanyName,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Status = customer.Status,
                OwnerId = customer.OwnerId
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CustomerCreateEditViewModel model)
        {
            if (id != model.CustomerId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                await PopulateUsersDropdownAsync(model.OwnerId);
                return View(model);
            }

            var customer = new Customer
            {
                CustomerId = model.CustomerId,
                CustomerName = model.CustomerName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName,
                Address = model.Address,
                City = model.City,
                State = model.State,
                Status = model.Status,
                OwnerId = model.OwnerId
            };

            var (success, errorMessage) = await _customerService.UpdateCustomerAsync(customer);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to update customer.");
                await PopulateUsersDropdownAsync(model.OwnerId);
                return View(model);
            }

            TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var (success, errorMessage) = await _customerService.DeleteOrDeactivateCustomerAsync(id);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Customer deactivated successfully.";
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
