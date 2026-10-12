using System;

namespace AcxiomCRM.Models.Entities
{
    public class AuditLog
    {
        public long AuditLogId { get; set; }
        
        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }

        public string Action { get; set; } = string.Empty; // Login, Failed Login, Logout, Create, Update, Delete, Role Change, Security
        public string EntityName { get; set; } = string.Empty; // Customer, Lead, Opportunity, FollowUp, User, Role, Auth
        public string? RecordId { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string? IpAddress { get; set; }
        public string Result { get; set; } = "Success"; // Success, Failure, Warning
        public string? Details { get; set; }
    }
}
