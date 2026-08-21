using Dapper;
using Inventory.Domain;
using Common.Utilities.Exceptions;
using Common.Utilities;

namespace Inventory.Infrastructure;

public class CustomersRepository(InventoryDbContext _DbContext) : ICustomersRepository
{
    public async Task<bool> CreateCustomer(Customer customer)
    {
        using var db = _DbContext.CreateConnection;
        bool ok;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                customer.Id = Guid.NewGuid();
                string sqlQuery = @"
                       INSERT INTO public.customers
                              ( id,  full_name, document_number, email,   cellphone,   is_active,  state,  created_by,  created,  modified_by, modified)
                        VALUES(@Id, @FullName,  @DocumentNumber, @Email, @Cellphone, @IsActive,  @State, @CreatedBy,  @Created, @ModifiedBy, @Modified);
                    ";

                var result = await db.ExecuteAsync(sqlQuery, customer);
                transaction.Commit();
                ok = true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally
        {
            db.Close();
        }
        return ok;
    }
    public async Task<int> UpdateCustomer(Customer customer)
    {
        using var db = _DbContext.CreateConnection;
        int numberRows = 0;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                string sqlQuery = @"
                        UPDATE public.customers
                           SET full_name= @FullName, 
                               document_number= @DocumentNumber, 
                               email= @Email, 
                               cellphone= @Cellphone, 
                               is_active= @IsActive, 
                               modified_by= @ModifiedBy, 
                               modified= @Modified
                         WHERE id= @Id;
                    ";
                numberRows = await db.ExecuteAsync(sqlQuery, customer);
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<int>(ex); }
        finally { db.Close(); }

        return numberRows;
    }
    public async Task<int> DeleteCustomer(Guid id, int idUserModified)
    {
        using var db = _DbContext.CreateConnection;
        int numberRows = 0;
        try
        {
            DateTime fechaActual = DateTime.UtcNow;
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                // El genérico es el que el POS precarga por defecto: borrarlo
                // dejaría al tenant sin forma de cobrar una venta sin cliente
                // identificado.
                bool isGeneric = await db.ExecuteScalarAsync<bool>(
                    "SELECT is_generic FROM public.customers WHERE id = @Id AND state;",
                    new { Id = id }, transaction);
                if (isGeneric)
                    // Sin repetir el nombre: es editable por el usuario, y tenerlo
                    // escrito acá ya hizo que el mensaje quedara desactualizado una vez.
                    throw new CustomException("No se puede eliminar el cliente genérico del punto de venta.", Common.Utilities.MessageTypes.Warning);

                string sqlQuery = @"
                        UPDATE public.customers
                           SET state = false,
                               modified_by = @ModifiedBy,
                               modified = @Modified
                         WHERE id = @Id ;
                    ";
                numberRows = await db.ExecuteAsync(sqlQuery, new { Id = id, ModifiedBy = idUserModified, @Modified = fechaActual });
                transaction.Commit();
            }
            catch (CustomException)
            {
                transaction.Rollback();
                throw;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }

        return numberRows;
    }

    public async Task<Customer> GetCustomer(Guid Id)
    {
        Customer customer = new();
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            string sqlQuery = @"
                    SELECT id, full_name, document_number, email, cellphone, is_active, is_generic
                         FROM public.customers
                        WHERE state
                          AND id = @Id;
                ";
            var result = await db.QueryAsync<Customer>(sqlQuery, new { id = Id });
            if (result!.ToList().Count > 0)
            {
                customer = result!.ToList().First();
            }
            else
            {
                throw new CustomException("No existe el cliente, de acuerdo al parametro ingresado", Common.Utilities.MessageTypes.Info);
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex, ex.messageType); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }
        return customer;
    }

    public async Task<List<Customer>> GetCustomers(string customerName)
    {
        List<Customer> listCustomers = new();
        using var db = _DbContext.CreateConnection;
        try
        {
            customerName = "%" + customerName + "%";
            db.Open();


            string sqlQuery = @"
                       SELECT id, full_name, document_number, email, cellphone, is_active, is_generic
                         FROM public.customers
                        WHERE state
                          AND full_name ILIKE @CustomerName;
                ";
            var result = await db.QueryAsync<Customer>(sqlQuery, new { CustomerName = customerName });
            listCustomers = result!.ToList();
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }

        return listCustomers;
    }

    public async Task<Customer> GetDefaultCustomer()
    {
        Customer customer = new();
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            string sqlQuery = @"
                    SELECT id, full_name, document_number, email, cellphone, is_active, is_generic
                         FROM public.customers
                        WHERE state
                          AND is_generic
                        LIMIT 1;
                ";
            var result = await db.QueryAsync<Customer>(sqlQuery);
            if (result!.ToList().Count > 0)
            {
                customer = result!.ToList().First();
            }
            else
            {
                throw new CustomException("No se encontró el cliente por defecto de esta farmacia.", Common.Utilities.MessageTypes.Warning);
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex, ex.messageType); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }
        return customer;
    }
}
