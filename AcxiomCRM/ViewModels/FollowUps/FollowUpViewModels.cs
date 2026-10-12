using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.ViewModels.FollowUps
{
    public class FollowUpListViewModel
    {
        public IEnumerable<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public string? StatusFilter { get; set; }
        public string? TypeFilter { get; set; }
        public bool UpcomingOnly { get; set; }
        public bool OverdueOnly { get; set; }
    }

    public class FollowUpCreateEditViewModel : IValidatableObject
    {
        public int FollowUpId { get; set; }

        [Display(Name = "Related Customer (Optional)")]
        public int? CustomerId { get; set; }

        [Display(Name = "Related Lead (Optional)")]
        public int? LeadId { get; set; }

        [Display(Name = "Related Opportunity (Optional)")]
        public int? OpportunityId { get; set; }

        [Required]
        [Display(Name = "Related Entity Type")]
        public string RelatedType { get; set; } = "Customer";

        [Required(ErrorMessage = "Follow-up Date is required.")]
        [Display(Name = "Follow-up Date & Time")]
        public DateTime FollowUpDate { get; set; } = DateTime.Today.AddDays(1).AddHours(10);

        [Required(ErrorMessage = "Follow-up Type is required.")]
        [Display(Name = "Type")]
        public string FollowUpType { get; set; } = "Call";

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
        public string Subject { get; set; } = string.Empty;

        public string? Remarks { get; set; }

        [Required]
        public string Status { get; set; } = "Planned";

        [Display(Name = "Assigned User")]
        public string? AssignedTo { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (FollowUpId == 0 && Status == "Planned" && FollowUpDate.Date < DateTime.Today)
            {
                yield return new ValidationResult(
                    "Follow-up date cannot be earlier than today for a new/planned activity.",
                    new[] { nameof(FollowUpDate) });
            }
        }
    }
}
