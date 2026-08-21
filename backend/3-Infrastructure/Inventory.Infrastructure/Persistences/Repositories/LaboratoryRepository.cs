using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;

namespace Inventory.Infrastructure;

public class LaboratoryRepository(InventoryDbContext _DbContext) : ILaboratoryRepository
{
    public async Task<bool> CreateLaboratory(Laboratory laboratory)
    {
        using var db = _DbContext.CreateConnection;
        bool ok;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                laboratory.Id = Guid.NewGuid();
                string sqlQuery = @"
                        INSERT INTO laboratories
                               (id, laboratory_name, description, direction, celular, is_active, state, created_by, created, modified_by, modified)
                        VALUES(@Id, @LaboratoryName, @Description,@Direction, @celular, @IsActive, @State, @CreatedBy, @Created, @ModifiedBy, @Modified);
                    ";

                var result = await db.ExecuteAsync(sqlQuery, laboratory);
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

    public async Task<int> UpdateLaboratory(Laboratory laboratory)
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
                        UPDATE laboratories
                           SET laboratory_name = @LaboratoryName, 
                               description = @Description, 
                               direction= @Direction, 
                               celular= @celular, 
                               is_active = @IsActive,
                               modified_by = @ModifiedBy, 
                               modified = @Modified
                         WHERE id = @Id;
                    ";
                numberRows = await db.ExecuteAsync(sqlQuery, laboratory);
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

    public async Task<List<Laboratory>> GetLaboratories(string laboratoryName)
    {
        List<Laboratory> listLaboratories = [];
        using var db = _DbContext.CreateConnection;
        try
        {
            laboratoryName = "%" + laboratoryName + "%";
            db.Open();

            string sqlQuery = @"
                        SELECT id, laboratory_name, description, direction, celular, is_active
                         FROM laboratories
                        WHERE state
                          AND laboratory_name ILIKE @laboratoryName;
                ";
            var result = await db.QueryAsync<Laboratory>(sqlQuery, new { laboratoryName });
            listLaboratories = result!.ToList();
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }

        return listLaboratories;
    }

    public async Task<Laboratory> GetLaboratory(Guid Id)
    {
        Laboratory laboratory = new();
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            string sqlQuery = @"
                    SELECT id, laboratory_name, description, direction, celular, is_active
                        FROM laboratories
                        WHERE state
                        AND id = @Id;
                ";
            var result = await db.QueryAsync<Laboratory>(sqlQuery, new { Id });
            if (result!.ToList().Count > 0)
            {
                laboratory = result!.ToList().First();
            }
            else
            {
                throw new CustomException("No existen laboratorios, de acuerdo a los parametros ingresados", Common.Utilities.MessageTypes.Info);
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex, ex.messageType); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }
        return laboratory;
    }

    public async Task<int> DeleteLaboratory(Guid id, int idUserModified)
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
                string sqlQuery = @"
                        UPDATE laboratories
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

}





