using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Interfaces;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/leads")]
    [Authorize]
    public class LeadsApiController : ControllerBase
    {
        private readonly ILeadService _leadService;

        public LeadsApiController(ILeadService leadService)
        {
            _leadService = leadService;
        }

        [HttpGet]
        public async Task<IActionResult> GetLeads([FromQuery] string? search, [FromQuery] string? status)
        {
            var leads = await _leadService.GetLeadsAsync(search, status);
            var dtos = leads.Select(l => new LeadDto
            {
                LeadId = l.LeadId,
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                Email = l.Email,
                Phone = l.Phone,
                CompanyName = l.CompanyName,
                Source = l.Source,
                Status = l.Status,
                Priority = l.Priority,
                ExpectedValue = l.ExpectedValue,
                AssignedTo = l.AssignedTo,
                AssignedUserName = l.AssignedUser?.FullName,
                CreatedDate = l.CreatedDate
            }).ToList();

            return Ok(ApiResponse<List<LeadDto>>.Ok(dtos));
        }

        [HttpPost]
        public async Task<IActionResult> CreateLead([FromBody] CreateLeadDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<LeadDto>.Fail("Invalid lead payload.",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
            }

            var lead = new Lead
            {
                LeadName = dto.LeadName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName,
                Source = dto.Source,
                Status = dto.Status,
                Priority = dto.Priority,
                ExpectedValue = dto.ExpectedValue,
                Notes = dto.Notes,
                AssignedTo = dto.AssignedTo
            };

            var (success, errorMessage, created) = await _leadService.CreateLeadAsync(lead);
            if (!success)
            {
                return BadRequest(ApiResponse<LeadDto>.Fail(errorMessage ?? "Unable to create lead."));
            }

            var resultDto = new LeadDto
            {
                LeadId = created!.LeadId,
                LeadCode = created.LeadCode,
                LeadName = created.LeadName,
                Email = created.Email,
                Phone = created.Phone,
                Status = created.Status,
                ExpectedValue = created.ExpectedValue,
                CreatedDate = created.CreatedDate
            };

            return StatusCode(201, ApiResponse<LeadDto>.Ok(resultDto, "Lead created successfully."));
        }
    }
}
