using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Seguridad.Domain.Entities;

namespace Seguridad.Infrastructure;

public class ModulesRepository(SeguridadDbContext _context) : IModulesRepository
{
    public async Task<int> CreateModule(Modules module)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                module.Id = await db.ExecuteScalarAsync<int>("select set_sequences_key(@TableName)", new { TableName = "sec.modules" }, transaction);

                string sqlQuery = @"
                    INSERT INTO sec.modules
                          (id, name_module, show_order, route, icon_css, state, created_by, created, modified_by, modified)
                   VALUES(@Id, @NameModule, @ShowOrder, @Route, @IconCss, @State, @CreatedBy, @Created, @ModifiedBy, @Modified)
                ";
                await db.ExecuteAsync(sqlQuery, module, transaction);
                transaction.Commit();
                return module.Id;
            }
            catch (CustomException ex)
            {
                transaction.Rollback();
                throw new CustomException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<string>(ex); }
        finally { db.Close(); }
    }

    public async Task<int> UpdateModule(Modules module)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            string sqlQuery = @"
                    UPDATE sec.modules
                       SET name_module = @NameModule, 
                           show_order = @ShowOrder, 
                           route = @Route,
                           icon_css = @IconCss,
                           modified_by = @ModifiedBy, 
                           modified = @Modified
                     WHERE id = @Id;
                ";
            return await db.ExecuteAsync(sqlQuery, module);
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<int>(ex); }
        finally { db.Close(); }
    }

    public async Task<int> DeleteModule(int id, int idUserModified)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            string sqlQuery = @"
                    UPDATE sec.modules
                       SET state = false,
                           modified_by = @ModifiedBy, 
                           modified = @Modified
                     WHERE id = @Id;
                ";
            return await db.ExecuteAsync(sqlQuery, new { Id = id, ModifiedBy = idUserModified, Modified = DateTime.Now });
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<int>(ex); }
        finally { db.Close(); }
    }

    public async Task<List<Modules>> GetModules(string nameModule)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            string sqlQuery = @"
                    SELECT * 
                      FROM sec.modules
                     WHERE state = true
                       AND name_module ILIKE @NameModule
                     ORDER BY show_order;
                ";
            var result = await db.QueryAsync<Modules>(sqlQuery, new { NameModule = "%" + nameModule + "%" });
            return result.ToList();
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<List<Modules>>(ex); }
        finally { db.Close(); }
    }

    public async Task<Modules> GetModule(int id)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            string sqlQuery = @"
                    SELECT * 
                      FROM sec.modules
                     WHERE state = true
                       AND id = @Id;
                ";
            var result = await db.QueryFirstOrDefaultAsync<Modules>(sqlQuery, new { Id = id });
            if (result == null) throw new CustomException("Módulo no encontrado", MessageTypes.Info);
            return result;
        }
        catch (CustomException ex) { throw; }
        catch (Exception ex) { throw ExceptionHandler.HandleException<Modules>(ex); }
        finally { db.Close(); }
    }
}
