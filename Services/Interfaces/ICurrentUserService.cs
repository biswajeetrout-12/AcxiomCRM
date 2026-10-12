namespace AcxiomCRM.Services.Interfaces
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        string? UserName { get; }
        string? UserEmail { get; }
        bool IsAuthenticated { get; }
        bool IsAdmin { get; }
        bool IsManager { get; }
        bool IsSalesExecutive { get; }
        string? PrimaryRole { get; }
        string? IpAddress { get; }
    }
}
