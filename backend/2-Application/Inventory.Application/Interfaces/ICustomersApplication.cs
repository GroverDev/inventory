using Inventory.Domain.Entities.Requests;
using Common.Utilities;
namespace Inventory.Application;

public interface ICustomersApplication
{
    public Task<Response<bool>> CreateCustomer(CustomerRequest customer, int createdBy);
    public Task<Response<bool>> UpdateCustomer(CustomerRequest customer, int modifiedBy);
    public Task<Response<bool>> DeleteCustomer(string id, int modifiedBy);
    public Task<Response<List<CustomerRequest>>> GetCustomers(string clientName);
    public Task<Response<CustomerRequest>> GetCustomer(string id);

}
