using Inventory.Domain.Entities.Requests;
using Common.Utilities;
namespace Inventory.Application;

public interface ICustomersApplication
{
    /// <summary>Devuelve el Id del cliente creado, para que el POS pueda
    /// seleccionarlo de inmediato sin una segunda búsqueda.</summary>
    public Task<Response<string>> CreateCustomer(CustomerRequest customer, int createdBy);
    public Task<Response<bool>> UpdateCustomer(CustomerRequest customer, int modifiedBy);
    public Task<Response<bool>> DeleteCustomer(string id, int modifiedBy);
    public Task<Response<List<CustomerRequest>>> GetCustomers(string clientName);
    public Task<Response<CustomerRequest>> GetCustomer(string id);

    /// <summary>El cliente genérico del tenant activo, que el POS
    /// precarga por defecto.</summary>
    public Task<Response<CustomerRequest>> GetDefaultCustomer();
}
