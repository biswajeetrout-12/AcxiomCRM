using System;

namespace AcxiomCRM.Models.Entities
{
    public class FollowUp
    {
        public int FollowUpId { get; set; }

        public int? CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }

        public int? LeadId { get; set; }
        public virtual Lead? Lead { get; set; }

        public int? OpportunityId { get; set; }
        public virtual Opportunity? Opportunity { get; set; }

        public string RelatedType { get; set; } = "Customer"; // Customer, Lead, Opportunity
        public DateTime FollowUpDate { get; set; }
        public string FollowUpType { get; set; } = "Call"; // Call, Meeting, Email, Demo, Task
        public string Subject { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public string Status { get; set; } = "Planned"; // Planned, Completed, Missed, Cancelled

        public string? AssignedTo { get; set; }
        public virtual ApplicationUser? AssignedUser { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
