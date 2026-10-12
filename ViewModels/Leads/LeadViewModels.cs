using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.ViewModels.Leads
{
    public class LeadListViewModel
    {
        public IEnumerable<Lead> Leads { get; set; } = new List<Lead>();
        public string? SearchTerm { get; set; }
        public string? StatusFilter { get; set; }
        public string? AssignedToFilter { get; set; }
    }

    public class LeadCreateEditViewModel
    {
        public int LeadId { get; set; }

        public string? LeadCode { get; set; }

        [Required(ErrorMessage = "Lead Name is mandatory.")]
        [StringLength(150, ErrorMessage = "Lead Name cannot exceed 150 characters.")]
        [Display(Name = "Lead Name")]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Enter a valid 10-digit phone number.")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(150, ErrorMessage = "Company Name cannot exceed 150 characters.")]
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [Required(ErrorMessage = "Lead Source is required.")]
        public string Source { get; set; } = "Website";

        [Required(ErrorMessage = "Lead Status is mandatory.")]
        public string Status { get; set; } = "New";

        [Required(ErrorMessage = "Priority is required.")]
        public string Priority { get; set; } = "Medium";

        [Required(ErrorMessage = "Expected Value must be specified.")]
        [Range(0, 100000000, ErrorMessage = "Expected value must be a valid non-negative number.")]
        [Display(Name = "Expected Value ($)")]
        public decimal ExpectedValue { get; set; }

        [Display(Name = "Assigned Sales Executive")]
        public string? AssignedTo { get; set; }

        public string? Notes { get; set; }
    }

    public class LeadDetailsViewModel
    {
        public Lead Lead { get; set; } = null!;
        public List<Opportunity> Opportunities { get; set; } = new();
        public List<FollowUp> FollowUps { get; set; } = new();
        public List<Activity> Activities { get; set; } = new();
    }

    public class LeadConvertViewModel
    {
        public int LeadId { get; set; }
        public string LeadName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Create Initial Opportunity?")]
        public bool CreateOpportunity { get; set; } = true;

        [Display(Name = "Opportunity Name")]
        public string? OpportunityName { get; set; }

        [Display(Name = "Opportunity Amount ($)")]
        [Range(1, 100000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        public decimal OpportunityAmount { get; set; }
    }
}
