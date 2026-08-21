using Mapster;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Inventory.Domain;
using Inventory.Domain.Entities.Requests;
using Inventory.Infrastructure;

namespace Inventory.Application;

public class CustomersApplication(ICustomersRepository _customerRepository) : ICustomersApplication
{
    public async Task<Response<string>> CreateCustomer(CustomerRequest customerRequest, int createdBy)
    {
        Response<string> respuesta = new();
        try
        {
            customerRequest.Id = Guid.Empty.ToString();
            var customer = customerRequest.Adapt<Customer>();
            customer.CreatedBy = customer.ModifiedBy = createdBy;
            customer.Created = customer.Modified = DateTime.UtcNow;
            customer.State = true;
            customer.IsActive = true;
            // Solo lo siembra sec.fn_seed_tenant_master_data; nunca por esta vía,
            // sin importar lo que traiga el request (CustomerRequest no expone
            // este campo, así que Adapt no puede haberlo llenado de todos modos).
            customer.IsGeneric = false;

            await _customerRepository.CreateCustomer(customer);
            // El repositorio asigna el Id nuevo sobre el mismo objeto antes de
            // insertar: no hace falta una segunda consulta para devolverlo.
            respuesta.Data = customer.Id.ToString();
            respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }
    public async Task<Response<bool>> UpdateCustomer(CustomerRequest customerRequest, int modifiedBy)
    {
        Response<bool> respuesta = new();
        try
        {
            var customer = customerRequest.Adapt<Customer>();
            customer.ModifiedBy = modifiedBy;
            customer.Modified = DateTime.UtcNow;

            var rowsAffected = await _customerRepository.UpdateCustomer(customer); 
                if (rowsAffected <= 0)
                    throw new CustomException("No se pudo modificar el cliente");
            respuesta.Data = respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }
    public async Task<Response<bool>> DeleteCustomer(string id, int modifiedBy)
    {
        Response<bool> respuesta = new();
        try
        {
            Guid customerId = Guid.Parse(id);
            
            var rowsAffected = await _customerRepository.DeleteCustomer(customerId, modifiedBy); 
            if (rowsAffected <= 0)
                throw new CustomException("No se pudo eliminar el cliente");
            respuesta.Data = respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }
    
    public async Task<Response<List<CustomerRequest>>> GetCustomers(string customerName)
    {
        Response<List<CustomerRequest>> customers = new() { Data = new() };
        try
        {
            var resp = await _customerRepository.GetCustomers(customerName); 
            foreach (var customer in resp){
                var customerRequest = customer.Adapt<CustomerRequest>();
                customers.Data.Add(customerRequest);
            }
          
            customers.ok = true;
        }
        catch (CustomException ex) { customers.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { customers.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return customers;
    }
    public async Task<Response<CustomerRequest>> GetCustomer(string id)
    {
        Response<CustomerRequest> customer = new() { Data = new() };
        try
        {
            Guid customerId = Guid.Parse(id);
            var respCustomer = await _customerRepository.GetCustomer(customerId);

            var customerNew = respCustomer.Adapt<CustomerRequest>();
            customer.Data = customerNew;
            customer.ok = true;
        }
        catch (CustomException ex) { customer.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { customer.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return customer;
    }

    public async Task<Response<CustomerRequest>> GetDefaultCustomer()
    {
        Response<CustomerRequest> customer = new() { Data = new() };
        try
        {
            var respCustomer = await _customerRepository.GetDefaultCustomer();
            customer.Data = respCustomer.Adapt<CustomerRequest>();
            customer.ok = true;
        }
        catch (CustomException ex) { customer.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { customer.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return customer;
    }
}
