using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.ViewModels.Activities
{
    public class ActivityListViewModel
    {
        public IEnumerable<Activity> Activities { get; set; } = new List<Activity>();
        public string? TypeFilter { get; set; }
        public string? StatusFilter { get; set; }
    }

    public class ActivityCreateViewModel
    {
        [Required(ErrorMessage = "Activity Type is required.")]
        [Display(Name = "Activity Type")]
        public string ActivityType { get; set; } = "Call"; // Call, Meeting, Email, Task

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
        public string Subject { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "Activity Date is required.")]
        [Display(Name = "Activity Date & Time")]
        public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Customer (Optional)")]
        public int? CustomerId { get; set; }

        [Display(Name = "Lead (Optional)")]
        public int? LeadId { get; set; }

        [Display(Name = "Assigned User")]
        public string? AssignedTo { get; set; }

        public string Status { get; set; } = "Completed";
    }
}
