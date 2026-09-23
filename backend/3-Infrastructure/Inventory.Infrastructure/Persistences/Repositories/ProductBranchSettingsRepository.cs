using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;

namespace Inventory.Infrastructure;

public class ProductBranchSettingsRepository(InventoryDbContext _DbContext) : IProductBranchSettingsRepository
{
    public async Task<List<ProductBranchSettingResponse>> GetSettings(Guid productId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            var result = await db.QueryAsync<ProductBranchSettingResponse>(@"
                SELECT b.id AS branch_id, b.name AS branch_name,
                       s.sale_price, s.min_reorder_quantity,
                       p.sale_price AS base_sale_price,
                       COALESCE(p.min_reorder_quantity, 0) AS base_min_reorder_quantity,
                       COALESCE(vs.quantity, 0) AS stock
                  FROM branches b
                  JOIN products p ON p.id = @productId
                  LEFT JOIN product_branch_settings s ON s.branch_id = b.id AND s.product_id = p.id
                  LEFT JOIN v_stock_sucursal vs ON vs.branch_id = b.id AND vs.product_id = p.id
                 WHERE b.state AND b.is_active
                 ORDER BY b.name",
                new { productId });
            return result.ToList();
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<List<ProductBranchSettingResponse>>(ex); }
        finally { db.Close(); }
    }

    public async Task SaveSettings(Guid productId, List<ProductBranchSettingRequest> settings, int userId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                bool existe = await db.ExecuteScalarAsync<bool>(
                    "SELECT EXISTS (SELECT 1 FROM products WHERE id = @productId AND state)",
                    new { productId }, transaction);
                if (!existe) throw new CustomException("No existe el producto.");

                foreach (var s in settings)
                {
                    if (s.SalePrice is null && s.MinReorderQuantity is null)
                    {
                        // Sin excepción: la sucursal vuelve a los valores base.
                        await db.ExecuteAsync(
                            "DELETE FROM product_branch_settings WHERE branch_id = @BranchId AND product_id = @productId",
                            new { s.BranchId, productId }, transaction);
                        continue;
                    }

                    // La FK compuesta rechaza una sucursal de otra farmacia.
                    await db.ExecuteAsync(@"
                        INSERT INTO product_branch_settings
                               (branch_id, product_id, sale_price, min_reorder_quantity, created_by, modified_by)
                        VALUES (@BranchId, @productId, @SalePrice, @MinReorderQuantity, @userId, @userId)
                        ON CONFLICT (branch_id, product_id) DO UPDATE
                           SET sale_price = EXCLUDED.sale_price,
                               min_reorder_quantity = EXCLUDED.min_reorder_quantity,
                               modified_by = EXCLUDED.modified_by,
                               modified = now()",
                        new { s.BranchId, productId, s.SalePrice, s.MinReorderQuantity, userId }, transaction);
                }

                transaction.Commit();
            }
            catch { transaction.Rollback(); throw; }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally { db.Close(); }
    }
}
