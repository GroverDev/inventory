using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;

namespace Inventory.Infrastructure;

public class CategoryRepository(InventoryDbContext _DbContext) : ICategoryRepository
{
    public async Task<bool> CreateCategory(Category category)
    {
        using var db = _DbContext.CreateConnection;
        bool ok;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                category.Id = Guid.NewGuid();
                string sqlQuery = @"
                        INSERT INTO categories
                               (id, category_name, description, is_active, state, created_by, created, modified_by, modified)
                        VALUES (@Id, @CategoryName, @Description, @IsActive, @State, @CreatedBy, @Created, @ModifiedBy, @Modified);
                    ";
                var result = await db.ExecuteAsync(sqlQuery, category);
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
        finally { db.Close(); }
        return ok;
    }

    public async Task<int> UpdateCategory(Category category)
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
                        UPDATE categories
                           SET category_name = @CategoryName,
                               description = @Description,
                               is_active = @IsActive,
                               modified_by = @ModifiedBy,
                               modified = @Modified
                         WHERE id = @Id;
                    ";
                numberRows = await db.ExecuteAsync(sqlQuery, category);
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

    public async Task<List<Category>> GetCategories(string categoryName)
    {
        List<Category> list = [];
        using var db = _DbContext.CreateConnection;
        try
        {
            categoryName = "%" + categoryName + "%";
            db.Open();
            string sqlQuery = @"
                        SELECT id, category_name, description, is_active
                          FROM categories
                         WHERE state
                           AND category_name ILIKE @categoryName;
                ";
            var result = await db.QueryAsync<Category>(sqlQuery, new { categoryName });
            list = result!.ToList();
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }
        return list;
    }

    public async Task<Category> GetCategory(Guid id)
    {
        Category category = new();
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            string sqlQuery = @"
                    SELECT id, category_name, description, is_active
                      FROM categories
                     WHERE state
                       AND id = @Id;
                ";
            var result = await db.QueryAsync<Category>(sqlQuery, new { Id = id });
            if (result!.ToList().Count > 0)
                category = result!.ToList().First();
            else
                throw new CustomException("No existe la categoría con los parámetros ingresados", Common.Utilities.MessageTypes.Info);
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex, ex.messageType); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }
        return category;
    }

    public async Task<int> DeleteCategory(Guid id, int idUserModified)
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
                        UPDATE categories
                           SET state = false,
                               modified_by = @ModifiedBy,
                               modified = @Modified
                         WHERE id = @Id;
                    ";
                numberRows = await db.ExecuteAsync(sqlQuery, new { Id = id, ModifiedBy = idUserModified, Modified = fechaActual });
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
