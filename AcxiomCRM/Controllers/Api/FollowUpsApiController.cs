using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Interfaces;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/followups")]
    [Authorize]
    public class FollowUpsApiController : ControllerBase
    {
        private readonly IFollowUpService _followUpService;

        public FollowUpsApiController(IFollowUpService followUpService)
        {
            _followUpService = followUpService;
        }

        [HttpGet]
        public async Task<IActionResult> GetFollowUps([FromQuery] string? status, [FromQuery] string? type)
        {
            var followUps = await _followUpService.GetFollowUpsAsync(status, type);
            var dtos = followUps.Select(f => new FollowUpDto
            {
                FollowUpId = f.FollowUpId,
                CustomerId = f.CustomerId,
                LeadId = f.LeadId,
                OpportunityId = f.OpportunityId,
                RelatedType = f.RelatedType,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Subject = f.Subject,
                Remarks = f.Remarks,
                Status = f.Status,
                AssignedTo = f.AssignedTo,
                AssignedUserName = f.AssignedUser?.FullName,
                CreatedDate = f.CreatedDate
            }).ToList();

            return Ok(ApiResponse<List<FollowUpDto>>.Ok(dtos));
        }

        [HttpPost]
        public async Task<IActionResult> CreateFollowUp([FromBody] CreateFollowUpDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<FollowUpDto>.Fail("Invalid follow-up payload.",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
            }

            var followUp = new FollowUp
            {
                CustomerId = dto.CustomerId > 0 ? dto.CustomerId : null,
                LeadId = dto.LeadId > 0 ? dto.LeadId : null,
                OpportunityId = dto.OpportunityId > 0 ? dto.OpportunityId : null,
                RelatedType = dto.RelatedType,
                FollowUpDate = dto.FollowUpDate,
                FollowUpType = dto.FollowUpType,
                Subject = dto.Subject,
                Remarks = dto.Remarks,
                Status = dto.Status,
                AssignedTo = dto.AssignedTo
            };

            var (success, errorMessage, created) = await _followUpService.CreateFollowUpAsync(followUp);
            if (!success)
            {
                return BadRequest(ApiResponse<FollowUpDto>.Fail(errorMessage ?? "Unable to create follow-up."));
            }

            var resultDto = new FollowUpDto
            {
                FollowUpId = created!.FollowUpId,
                Subject = created.Subject,
                FollowUpDate = created.FollowUpDate,
                FollowUpType = created.FollowUpType,
                Status = created.Status,
                CreatedDate = created.CreatedDate
            };

            return StatusCode(201, ApiResponse<FollowUpDto>.Ok(resultDto, "Follow-up created successfully."));
        }
    }
}
