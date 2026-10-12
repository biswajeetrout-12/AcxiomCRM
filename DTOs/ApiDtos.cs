using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.DTOs
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();

        public static ApiResponse<T> Ok(T data, string message = "Success") =>
            new() { Success = true, Message = message, Data = data };

        public static ApiResponse<T> Fail(string message, List<string>? errors = null) =>
            new() { Success = false, Message = message, Errors = errors ?? new() };
    }

    public class CustomerDto
    {
        public int CustomerId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string Status { get; set; } = "Active";
        public string? OwnerId { get; set; }
        public string? OwnerName { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class CreateCustomerDto
    {
        [Required]
        [StringLength(150)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone must be a 10-digit number.")]
        public string Phone { get; set; } = string.Empty;

        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string Status { get; set; } = "Active";
        public string? OwnerId { get; set; }
    }

    public class LeadDto
    {
        public int LeadId { get; set; }
        public string LeadCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public decimal ExpectedValue { get; set; }
        public string? AssignedTo { get; set; }
        public string? AssignedUserName { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class CreateLeadDto
    {
        [Required]
        [StringLength(150)]
        public string LeadName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone must be a 10-digit number.")]
        public string Phone { get; set; } = string.Empty;

        public string? CompanyName { get; set; }
        public string Source { get; set; } = "Website";
        public string Status { get; set; } = "New";
        public string Priority { get; set; } = "Medium";

        [Range(0, 100000000)]
        public decimal ExpectedValue { get; set; }
        public string? Notes { get; set; }
        public string? AssignedTo { get; set; }
    }

    public class OpportunityDto
    {
        public int OpportunityId { get; set; }
        public string OpportunityName { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? LeadId { get; set; }
        public decimal Amount { get; set; }
        public string Stage { get; set; } = string.Empty;
        public int Probability { get; set; }
        public decimal WeightedValue => (Amount * Probability) / 100m;
        public DateTime ExpectedCloseDate { get; set; }
        public string Status { get; set; } = "Open";
        public string? AssignedTo { get; set; }
        public string? AssignedUserName { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class CreateOpportunityDto
    {
        [Required]
        [StringLength(150)]
        public string OpportunityName { get; set; } = string.Empty;

        [Required]
        public int CustomerId { get; set; }
        public int? LeadId { get; set; }

        [Required]
        [Range(0.01, 100000000, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required]
        public string Stage { get; set; } = "Qualification";

        [Range(0, 100)]
        public int Probability { get; set; } = 20;

        [Required]
        public DateTime ExpectedCloseDate { get; set; }
        public string Status { get; set; } = "Open";
        public string? AssignedTo { get; set; }
        public string? Notes { get; set; }
    }

    public class FollowUpDto
    {
        public int FollowUpId { get; set; }
        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }
        public int? OpportunityId { get; set; }
        public string RelatedType { get; set; } = string.Empty;
        public DateTime FollowUpDate { get; set; }
        public string FollowUpType { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? AssignedTo { get; set; }
        public string? AssignedUserName { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class CreateFollowUpDto
    {
        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }
        public int? OpportunityId { get; set; }
        public string RelatedType { get; set; } = "Customer";

        [Required]
        public DateTime FollowUpDate { get; set; }

        [Required]
        public string FollowUpType { get; set; } = "Call";

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        public string? Remarks { get; set; }
        public string Status { get; set; } = "Planned";
        public string? AssignedTo { get; set; }
    }

    public class LoginRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
