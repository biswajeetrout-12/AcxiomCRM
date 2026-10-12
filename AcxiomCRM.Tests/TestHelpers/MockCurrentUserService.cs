using AcxiomCRM.Services.Interfaces;

namespace AcxiomCRM.Tests.TestHelpers
{
    public class MockCurrentUserService : ICurrentUserService
    {
        public string? UserId { get; set; } = "admin-user-id";
        public string? UserName { get; set; } = "System Administrator";
        public string? UserEmail { get; set; } = "admin@acxiomcrm.com";
        public string? Role { get; set; } = "Admin";
        public string? PrimaryRole => Role;
        public bool IsAuthenticated { get; set; } = true;
        public bool IsAdmin => Role == "Admin";
        public bool IsManager => Role == "Manager";
        public bool IsSalesExecutive => Role == "SalesExecutive";
        public string? IpAddress { get; set; } = "127.0.0.1";

        public void SetUser(string userId, string userEmail, string role, string? fullName = null)
        {
            UserId = userId;
            UserEmail = userEmail;
            UserName = fullName ?? userEmail;
            Role = role;
            IsAuthenticated = true;
        }

        public void SetAnonymous()
        {
            UserId = null;
            UserName = null;
            UserEmail = null;
            Role = null;
            IsAuthenticated = false;
        }
    }
}
