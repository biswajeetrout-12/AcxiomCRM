using System;
using System.Collections.Generic;
using AcxiomCRM.Models.Enums;

namespace AcxiomCRM.Models.Entities
{
    public class Lead
    {
        public int LeadId { get; set; }
        public string LeadCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Source { get; set; } = "Website";
        public string Status { get; set; } = "New"; // New, Contacted, Qualified, Unqualified, Converted, Lost
        public string Priority { get; set; } = "Medium"; // Low, Medium, High, Urgent
        public decimal ExpectedValue { get; set; } = 0;
        public string? Notes { get; set; }

        public string? AssignedTo { get; set; }
        public virtual ApplicationUser? AssignedUser { get; set; }

        public int? ConvertedCustomerId { get; set; }
        public virtual Customer? ConvertedCustomer { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedDate { get; set; }

        // Navigation
        public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
        public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
    }
}
