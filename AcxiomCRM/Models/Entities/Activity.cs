using System;

namespace AcxiomCRM.Models.Entities
{
    public class Activity
    {
        public int ActivityId { get; set; }
        public string ActivityType { get; set; } = "Call"; // Call, Meeting, Email, Task
        public string Subject { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

        public int? CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }

        public int? LeadId { get; set; }
        public virtual Lead? Lead { get; set; }

        public string? AssignedTo { get; set; }
        public virtual ApplicationUser? AssignedUser { get; set; }

        public string Status { get; set; } = "Completed"; // Completed, Pending, Cancelled
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
