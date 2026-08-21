using Common.Utilities;
using Dapper;
using Seguridad.Domain;
using Common.Utilities.Exceptions;

namespace Seguridad.Infrastructure;

public class FormsRepository(SeguridadDbContext _context) : IFormsRepository
{
    public async Task<List<Forms>> GetFormsXRolId(int rolId)
    {
        var formularios = new List<Forms>();
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            string query = @"select f.id,
                                      f.form_id,
                                      f.show_order,
                                      f.name_form ,
                                      f.description ,
                                      f.icon_css ,
                                      f.show_menu ,
                                      f.state ,
                                      f.is_form_register ,
                                      f.route  ,
                                      COALESCE(rf.can_create, true) as can_create,
                                      COALESCE(rf.can_read,   true) as can_read,
                                      COALESCE(rf.can_update, true) as can_update,
                                      COALESCE(rf.can_delete, true) as can_delete
                                 from sec.forms f
                                      inner join sec.roles_forms rf
                                   on rf.form_id  = f.id
                                where rf.rol_id  = @IdRol
                                  and f.state
                                  and rf.state ";
            var resultFormularios = await db.QueryAsync<Forms>(query, new { IdRol = rolId });
            formularios = resultFormularios.ToList();
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<List<Forms>>(ex); }
        finally { db.Close(); }
        return formularios;
    }

    public async Task<int> CreateForm(Forms form)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                form.Id = await db.ExecuteScalarAsync<int>("select set_sequences_key(@TableName)", new { TableName = "sec.forms" }, transaction);

                string sqlQuery = @"
                    INSERT INTO sec.forms
                          (id, form_id, name_form, description, show_order, route, controller, icon_css, show_menu, is_form_register, module_id, state, created_by, created, modified_by, modified)
                   VALUES(@Id, @FormId, @NameForm, @Description, @ShowOrder, @Route, @Controller, @IconCss, @ShowMenu, @IsFormRegister, @ModuleId, @State, @CreatedBy, @Created, @ModifiedBy, @Modified)
                ";
                await db.ExecuteAsync(sqlQuery, form, transaction);
                transaction.Commit();
                return form.Id;
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

    public async Task<int> UpdateForm(Forms form)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            string sqlQuery = @"
                    UPDATE sec.forms
                       SET form_id = @FormId, 
                           name_form = @NameForm, 
                           description = @Description, 
                           show_order = @ShowOrder, 
                           route = @Route,
                           controller = @Controller,
                           icon_css = @IconCss,
                           show_menu = @ShowMenu,
                           is_form_register = @IsFormRegister,
                           module_id = @ModuleId,
                           modified_by = @ModifiedBy, 
                           modified = @Modified
                     WHERE id = @Id;
                ";
            return await db.ExecuteAsync(sqlQuery, form);
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<int>(ex); }
        finally { db.Close(); }
    }

    public async Task<int> DeleteForm(int id, int idUserModified)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            string sqlQuery = @"
                    UPDATE sec.forms
                       SET state = false,
                           modified_by = @ModifiedBy, 
                           modified = @Modified
                     WHERE id = @Id;
                ";
            return await db.ExecuteAsync(sqlQuery, new { Id = id, ModifiedBy = idUserModified, Modified = DateTime.UtcNow });
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<int>(ex); }
        finally { db.Close(); }
    }

    public async Task<List<Forms>> GetForms(string nameForm)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            string sqlQuery = @"
                    SELECT * 
                      FROM sec.forms
                     WHERE state = true
                       AND name_form ILIKE @NameForm
                     ORDER BY name_form;
                ";
            var result = await db.QueryAsync<Forms>(sqlQuery, new { NameForm = "%" + nameForm + "%" });
            return result.ToList();
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<List<Forms>>(ex); }
        finally { db.Close(); }
    }

    public async Task<Forms> GetForm(int id)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            string sqlQuery = @"
                    SELECT * 
                      FROM sec.forms
                     WHERE state = true
                       AND id = @Id;
                ";
            var result = await db.QueryFirstOrDefaultAsync<Forms>(sqlQuery, new { Id = id });
            if (result == null) throw new CustomException("Formulario no encontrado", MessageTypes.Info);
            return result;
        }
        catch (CustomException ex) { throw ExceptionHandler.HandleException<Forms>(ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<Forms>(ex); }
        finally { db.Close(); }
    }
}
