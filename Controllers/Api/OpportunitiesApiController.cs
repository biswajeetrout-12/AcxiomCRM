using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Interfaces;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/opportunities")]
    [Authorize]
    public class OpportunitiesApiController : ControllerBase
    {
        private readonly IOpportunityService _opportunityService;

        public OpportunitiesApiController(IOpportunityService opportunityService)
        {
            _opportunityService = opportunityService;
        }

        [HttpGet]
        public async Task<IActionResult> GetOpportunities([FromQuery] string? search, [FromQuery] string? stage, [FromQuery] string? status)
        {
            var opps = await _opportunityService.GetOpportunitiesAsync(search, stage, status);
            var dtos = opps.Select(o => new OpportunityDto
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer?.CustomerName,
                LeadId = o.LeadId,
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status,
                AssignedTo = o.AssignedTo,
                AssignedUserName = o.AssignedUser?.FullName,
                CreatedDate = o.CreatedDate
            }).ToList();

            return Ok(ApiResponse<List<OpportunityDto>>.Ok(dtos));
        }

        [HttpPost]
        public async Task<IActionResult> CreateOpportunity([FromBody] CreateOpportunityDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<OpportunityDto>.Fail("Invalid opportunity payload.",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
            }

            var opp = new Opportunity
            {
                OpportunityName = dto.OpportunityName,
                CustomerId = dto.CustomerId,
                LeadId = dto.LeadId,
                Amount = dto.Amount,
                Stage = dto.Stage,
                Probability = dto.Probability,
                ExpectedCloseDate = dto.ExpectedCloseDate,
                Status = dto.Status,
                AssignedTo = dto.AssignedTo,
                Notes = dto.Notes
            };

            var (success, errorMessage, created) = await _opportunityService.CreateOpportunityAsync(opp);
            if (!success)
            {
                return BadRequest(ApiResponse<OpportunityDto>.Fail(errorMessage ?? "Unable to create opportunity."));
            }

            var resultDto = new OpportunityDto
            {
                OpportunityId = created!.OpportunityId,
                OpportunityName = created.OpportunityName,
                CustomerId = created.CustomerId,
                Amount = created.Amount,
                Stage = created.Stage,
                Probability = created.Probability,
                ExpectedCloseDate = created.ExpectedCloseDate,
                Status = created.Status,
                CreatedDate = created.CreatedDate
            };

            return StatusCode(201, ApiResponse<OpportunityDto>.Ok(resultDto, "Opportunity created successfully."));
        }
    }
}
