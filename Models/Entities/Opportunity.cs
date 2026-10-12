using System;
using System.Collections.Generic;

namespace AcxiomCRM.Models.Entities
{
    public class Opportunity
    {
        public int OpportunityId { get; set; }
        public string OpportunityName { get; set; } = string.Empty;

        public int CustomerId { get; set; }
        public virtual Customer Customer { get; set; } = null!;

        public int? LeadId { get; set; }
        public virtual Lead? Lead { get; set; }

        public decimal Amount { get; set; }
        public string Stage { get; set; } = "Qualification"; // Qualification, Proposal, Negotiation, Won, Lost
        public int Probability { get; set; } = 20; // 0 to 100
        public DateTime ExpectedCloseDate { get; set; }
        public string Status { get; set; } = "Open"; // Open, Won, Lost, Abandoned
        public string? Notes { get; set; }

        public string? AssignedTo { get; set; }
        public virtual ApplicationUser? AssignedUser { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedDate { get; set; }

        // Derived property
        public decimal WeightedValue => (Amount * Probability) / 100m;

        // Navigation
        public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    }
}
