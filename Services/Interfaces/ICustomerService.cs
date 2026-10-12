using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.Services.Interfaces
{
    public interface ICustomerService
    {
        Task<IEnumerable<Customer>> GetCustomersAsync(string? search = null, string? status = null);
        Task<Customer?> GetCustomerByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage, Customer? Customer)> CreateCustomerAsync(Customer customer);
        Task<(bool Success, string? ErrorMessage)> UpdateCustomerAsync(Customer customer);
        Task<(bool Success, string? ErrorMessage)> DeleteOrDeactivateCustomerAsync(int id);
        Task<bool> IsEmailUniqueAsync(string email, int? currentCustomerId = null);
        Task<bool> IsPhoneUniqueAsync(string phone, int? currentCustomerId = null);
    }
}
