using Inventory.Domain;

namespace Inventory.Infrastructure;

public interface ICustomersRepository
{
    Task<bool> CreateCustomer(Customer customer);
    Task<int> UpdateCustomer(Customer customer);
    Task<List<Customer>> GetCustomers(string customerName);
    Task<Customer> GetCustomer(Guid Id);
    Task<int> DeleteCustomer(Guid id, int idUserModified);
}
