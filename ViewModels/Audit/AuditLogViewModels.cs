using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.ViewModels.Audit
{
    public class AuditLogListViewModel
    {
        public IEnumerable<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public string? ModuleFilter { get; set; }
        public string? ActionFilter { get; set; }
        public string? UserFilter { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
