using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;

namespace Inventory.Infrastructure;

public class ProviderRepository(InventoryDbContext _DbContext): IProviderRepository
{
    public async Task<bool> CreateProvider(Provider provider)
    {
        using var db = _DbContext.CreateConnection;
        bool ok;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                provider.Id = Guid.NewGuid();
                string sqlQuery = @"
                        INSERT INTO providers
                        (id, provider_name, description, direction, celular, is_company, is_active, state, created_by, created, modified_by, modified)
                        VALUES(@Id, @ProviderName, @Description,@Direction, @celular, @IsCompany, @IsActive, @State, @CreatedBy, @Created, @ModifiedBy, @Modified);
                    ";
                var result = await db.ExecuteAsync(sqlQuery, provider);
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

    public async Task<int> UpdateProvider(Provider provider)
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
                        UPDATE providers
                           SET provider_name = @ProviderName, 
                               description = @Description, 
                               direction= @Direction, 
                               celular= @celular, 
                               modified_by = @ModifiedBy, 
                               modified = @Modified
                         WHERE id = @Id ;
                    ";
                numberRows = await db.ExecuteAsync(sqlQuery, provider);
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

    public async Task<int> DeleteProvider(Guid id, int idUserModified)
    {
        using var db = _DbContext.CreateConnection;
        int numberRows = 0;
        try
        {
            DateTime fechaActual = DateTime.Now;
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                string sqlQuery = @"
                        UPDATE providers
                           SET state = false,
                               modified_by = @ModifiedBy, 
                               modified = @Modified
                         WHERE id = @Id ;
                    ";
                numberRows = await db.ExecuteAsync(sqlQuery, new { Id = id, ModifiedBy = idUserModified, @Modified = fechaActual });
                transaction.Commit();
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

    public async Task<Provider> GetProvider(Guid Id)
    {
        Provider provider = new();
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            string sqlQuery = @"
                    SELECT id, provider_name, description, direction, celular
                        FROM providers
                        WHERE state
                        AND id = @Id;
                ";
            var result = await db.QueryAsync<Provider>(sqlQuery, new { id = Id });
            if (result!.ToList().Count > 0)
            {
                provider = result!.ToList().First();
            }
            else
            {
                throw new CustomException("No existen resultados, de acuerdo a los parametros ingresados", Common.Utilities.MessageTypes.Info);
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex, ex.messageType); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }
        return provider;
    }

    public async Task<List<Provider>> GetProviders(string providerName)
    {
        List<Provider> listProviders = new();
        using var db = _DbContext.CreateConnection;
        try
        {
            providerName = "%" + providerName + "%";
            db.Open();


            string sqlQuery = @"
                       SELECT id, provider_name, description, direction, celular
                         FROM providers
                        WHERE state
                          AND provider_name ILIKE @ProviderName;
                ";
            var result = await db.QueryAsync<Provider>(sqlQuery, new { ProviderName = providerName });
            listProviders = result!.ToList();
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }

        return listProviders;
    }


}
