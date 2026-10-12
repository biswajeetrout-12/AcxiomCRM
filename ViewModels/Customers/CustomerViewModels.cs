using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.ViewModels.Customers
{
    public class CustomerListViewModel
    {
        public IEnumerable<Customer> Customers { get; set; } = new List<Customer>();
        public string? SearchTerm { get; set; }
        public string? StatusFilter { get; set; }
    }

    public class CustomerCreateEditViewModel
    {
        public int CustomerId { get; set; }

        public string? CustomerCode { get; set; }

        [Required(ErrorMessage = "Customer Name is required.")]
        [StringLength(150, ErrorMessage = "Customer Name cannot exceed 150 characters.")]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Enter a valid 10-digit phone number.")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(150, ErrorMessage = "Company Name cannot exceed 150 characters.")]
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [StringLength(250, ErrorMessage = "Address cannot exceed 250 characters.")]
        public string? Address { get; set; }

        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
        public string? City { get; set; }

        [StringLength(100, ErrorMessage = "State cannot exceed 100 characters.")]
        public string? State { get; set; }

        [Required]
        public string Status { get; set; } = "Active";

        [Display(Name = "Assigned Sales Executive")]
        public string? OwnerId { get; set; }

        public string? Notes { get; set; }
    }

    public class CustomerDetailsViewModel
    {
        public Customer Customer { get; set; } = null!;
        public List<Opportunity> Opportunities { get; set; } = new();
        public List<FollowUp> FollowUps { get; set; } = new();
        public List<Activity> Activities { get; set; } = new();
    }
}
