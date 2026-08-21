using Inventory.Domain;

namespace Inventory.Infrastructure;

public interface ICustomersRepository
{
    Task<bool> CreateCustomer(Customer customer);
    Task<int> UpdateCustomer(Customer customer);
    Task<List<Customer>> GetCustomers(string customerName);
    Task<Customer> GetCustomer(Guid Id);
    Task<int> DeleteCustomer(Guid id, int idUserModified);

    /// <summary>El cliente genérico del tenant activo, sembrado por
    /// sec.fn_seed_tenant_master_data. El POS lo precarga para no bloquear una
    /// venta sin cliente identificado.</summary>
    Task<Customer> GetDefaultCustomer();
}
