using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Interfaces;

namespace AcxiomCRM.Services.Implementations
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<AuditService> _logger;

        public AuditService(
            ApplicationDbContext context,
            ICurrentUserService currentUserService,
            ILogger<AuditService> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task LogAsync(
            string action,
            string entityName,
            string? recordId = null,
            string? oldValue = null,
            string? newValue = null,
            string result = "Success",
            string? details = null,
            string? customUserId = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = customUserId ?? _currentUserService.UserId,
                    Action = action,
                    EntityName = entityName,
                    RecordId = recordId,
                    OldValue = oldValue,
                    NewValue = newValue,
                    Result = result,
                    Details = details,
                    IpAddress = _currentUserService.IpAddress,
                    CreatedDate = DateTime.UtcNow
                };

                await _context.AuditLogs.AddAsync(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record audit log for action: {Action}, entity: {Entity}", action, entityName);
            }
        }
    }
}
