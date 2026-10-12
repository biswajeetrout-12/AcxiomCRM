using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Interfaces;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/customers")]
    [Authorize]
    public class CustomersApiController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomersApiController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers([FromQuery] string? search, [FromQuery] string? status)
        {
            var customers = await _customerService.GetCustomersAsync(search, status);
            var dtos = customers.Select(c => new CustomerDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                Address = c.Address,
                City = c.City,
                State = c.State,
                Status = c.Status,
                OwnerId = c.OwnerId,
                OwnerName = c.Owner?.FullName,
                CreatedDate = c.CreatedDate
            }).ToList();

            return Ok(ApiResponse<List<CustomerDto>>.Ok(dtos));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomer(int id)
        {
            var c = await _customerService.GetCustomerByIdAsync(id);
            if (c == null)
            {
                return NotFound(ApiResponse<CustomerDto>.Fail("Customer not found."));
            }

            var dto = new CustomerDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                Address = c.Address,
                City = c.City,
                State = c.State,
                Status = c.Status,
                OwnerId = c.OwnerId,
                OwnerName = c.Owner?.FullName,
                CreatedDate = c.CreatedDate
            };

            return Ok(ApiResponse<CustomerDto>.Ok(dto));
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<CustomerDto>.Fail("Invalid customer payload.",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
            }

            var customer = new Customer
            {
                CustomerName = dto.CustomerName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName,
                Address = dto.Address,
                City = dto.City,
                State = dto.State,
                Status = dto.Status,
                OwnerId = dto.OwnerId
            };

            var (success, errorMessage, created) = await _customerService.CreateCustomerAsync(customer);
            if (!success)
            {
                return Conflict(ApiResponse<CustomerDto>.Fail(errorMessage ?? "Unable to create customer."));
            }

            var resultDto = new CustomerDto
            {
                CustomerId = created!.CustomerId,
                CustomerCode = created.CustomerCode,
                CustomerName = created.CustomerName,
                Email = created.Email,
                Phone = created.Phone,
                CompanyName = created.CompanyName,
                Status = created.Status,
                OwnerId = created.OwnerId,
                CreatedDate = created.CreatedDate
            };

            return CreatedAtAction(nameof(GetCustomer), new { id = resultDto.CustomerId }, ApiResponse<CustomerDto>.Ok(resultDto, "Customer created."));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CreateCustomerDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Invalid customer payload."));
            }

            var customer = new Customer
            {
                CustomerId = id,
                CustomerName = dto.CustomerName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName,
                Address = dto.Address,
                City = dto.City,
                State = dto.State,
                Status = dto.Status,
                OwnerId = dto.OwnerId
            };

            var (success, errorMessage) = await _customerService.UpdateCustomerAsync(customer);
            if (!success)
            {
                if (errorMessage == "Customer not found.")
                {
                    return NotFound(ApiResponse<string>.Fail(errorMessage));
                }
                if (errorMessage?.Contains("not authorized") == true)
                {
                    return StatusCode(403, ApiResponse<string>.Fail(errorMessage));
                }
                return Conflict(ApiResponse<string>.Fail(errorMessage ?? "Update failed."));
            }

            return Ok(ApiResponse<string>.Ok("Customer updated successfully."));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var (success, errorMessage) = await _customerService.DeleteOrDeactivateCustomerAsync(id);
            if (!success)
            {
                if (errorMessage == "Customer not found.")
                {
                    return NotFound(ApiResponse<string>.Fail(errorMessage));
                }
                return StatusCode(403, ApiResponse<string>.Fail(errorMessage ?? "Delete failed."));
            }

            return Ok(ApiResponse<string>.Ok("Customer deactivated successfully."));
        }
    }
}
