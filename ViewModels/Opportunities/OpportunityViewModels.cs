using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.ViewModels.Opportunities
{
    public class OpportunityListViewModel
    {
        public IEnumerable<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
        public string? SearchTerm { get; set; }
        public string? StageFilter { get; set; }
        public string? StatusFilter { get; set; }
        public decimal TotalPipelineValue { get; set; }
        public decimal WeightedPipelineValue { get; set; }
    }

    public class OpportunityCreateEditViewModel : IValidatableObject
    {
        public int OpportunityId { get; set; }

        [Required(ErrorMessage = "Opportunity Name is required.")]
        [StringLength(150, ErrorMessage = "Opportunity Name cannot exceed 150 characters.")]
        [Display(Name = "Opportunity Name")]
        public string OpportunityName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Associated Customer is required.")]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }

        [Display(Name = "Source Lead (Optional)")]
        public int? LeadId { get; set; }

        [Required(ErrorMessage = "Opportunity Amount is required.")]
        [Range(0.01, 100000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        [Display(Name = "Amount ($)")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Stage is required.")]
        public string Stage { get; set; } = "Qualification";

        [Required(ErrorMessage = "Probability is required.")]
        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        [Display(Name = "Probability (%)")]
        public int Probability { get; set; } = 20;

        [Required(ErrorMessage = "Expected Close Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Expected Close Date")]
        public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);

        [Required]
        public string Status { get; set; } = "Open";

        [Display(Name = "Assigned Sales Executive")]
        public string? AssignedTo { get; set; }

        public string? Notes { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Status == "Open" && ExpectedCloseDate.Date < DateTime.Today)
            {
                yield return new ValidationResult(
                    "Expected Close Date cannot be in the past for an active Opportunity.",
                    new[] { nameof(ExpectedCloseDate) });
            }

            if (Amount <= 0)
            {
                yield return new ValidationResult(
                    "Opportunity Amount must be greater than 0.",
                    new[] { nameof(Amount) });
            }
        }
    }

    public class OpportunityDetailsViewModel
    {
        public Opportunity Opportunity { get; set; } = null!;
        public List<FollowUp> FollowUps { get; set; } = new();
    }
}
