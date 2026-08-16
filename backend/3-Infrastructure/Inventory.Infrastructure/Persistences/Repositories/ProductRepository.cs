using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;
using Inventory.Domain.Entities.Responses;

namespace Inventory.Infrastructure;

public class ProductRepository(InventoryDbContext _DbContext): IProductRepository
{
    public async Task<string> CreateProduct(Product product)
    {
        using var db = _DbContext.CreateConnection;
        
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                product.Id = Guid.NewGuid();
                 string sqlQuery = @"
                SELECT CASE WHEN EXISTS(SELECT 1 
                                          FROM products
                                         WHERE product_code = @ProductCode 
                                           AND state = true)
                            THEN CAST(1 as BIT) 
                            ELSE CAST(0 as BIT) 
                       END ";
                bool existeCodigoProducto = await db.QuerySingleAsync<bool>(sqlQuery, new { ProductCode = product.ProductCode });

                if (existeCodigoProducto)
                {
                    throw new CustomException("El codigo de producto ya existe, por favor verifique el codigo", MessageTypes.Warning);
                }
                sqlQuery = @"
                        INSERT INTO products
                              (id, product_name, description, sale_price, bar_code, product_code, current_stock, min_reorder_quantity,
                               available_in_pos, laboratory_id, category_id, uom_id, is_active, state, created_by, created, modified_by, modified)
                       VALUES(@Id, @ProductName, @Description, @SalePrice, @BarCode, @ProductCode, @CurrentStock, @MinReorderQuantity,
                               @AvailableInPos, @LaboratoryId, @CategoryId, @UomId, @IsActive, @State, @CreatedBy, @Created, @ModifiedBy, @Modified);
                    ";

                var result = await db.ExecuteAsync(sqlQuery, product);
                transaction.Commit();
            }
            catch (CustomException ex) { transaction.Rollback(); throw new CustomException(ex.Message, ex); }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally { db.Close(); }
        return product.Id.ToString();
    }

    public async Task<int> UpdateProduct(Product product)
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
                        UPDATE products
                           SET product_name = @ProductName,
                               product_code = @ProductCode,
                               description = @Description,
                               sale_price = @SalePrice,
                               min_reorder_quantity = @MinReorderQuantity,
                               available_in_pos = @AvailableInPos,
                               bar_code = @BarCode,
                               laboratory_id = @LaboratoryId,
                               category_id = @CategoryId,
                               uom_id = @UomId,
                               is_active = @IsActive,
                               modified_by = @ModifiedBy,
                               modified = @Modified
                         WHERE id = @Id;
                    ";

                numberRows = await db.ExecuteAsync(sqlQuery, product);
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

    public async Task<int> DeleteProduct(Guid id, int idUserModified)
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
                        UPDATE products
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

    public async Task<List<ProductResponse>> GetProducts(string productName)
    {
        List<ProductResponse> listProducts = new();
        using var db = _DbContext.CreateConnection;
        try
        {
            productName = "%" + productName + "%";
            db.Open();
            string sqlQuery = @"
                      SELECT p.id,
                              p.product_code,
                              p.product_name,
                              p.description,
                              p.sale_price,
                              p.is_active,
                              p.current_stock,
                              p.min_reorder_quantity,
                              p.bar_code,
                              p.laboratory_id,
                              l.laboratory_name,
                              p.category_id,
                              c.category_name,
                              p.uom_id,
                              uom.unit_name
                         FROM products p
                              LEFT  JOIN laboratories l ON p.laboratory_id = l.id
                              LEFT JOIN categories c ON p.category_id = c.id
                              INNER JOIN unit_of_measurement uom ON uom.id = p.uom_id
                        WHERE p.state
                          AND product_name ILIKE @ProductName;
                ";
            var result = await db.QueryAsync<ProductResponse>(sqlQuery, new { ProductName = productName });
            listProducts = result!.ToList();
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }

        return listProducts;
    }

    public async Task<(List<ProductResponse> Items, int TotalCount)> GetProductsStock(string productName, int page, int pageSize)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            int offset = (page - 1) * pageSize;
            var nameParam = string.IsNullOrWhiteSpace(productName) ? "" : "%" + productName.Trim() + "%";

            string sql = @"
                SELECT COUNT(*)
                  FROM products p
                 WHERE p.state
                   AND (@NameParam = '' OR p.product_name ILIKE @NameParam);

                SELECT p.id,
                       p.product_code,
                       p.product_name,
                       p.description,
                       p.sale_price,
                       p.bar_code,
                       p.available_in_pos,
                       p.is_active,
                       p.current_stock,
                       p.min_reorder_quantity,
                       p.laboratory_id,
                       l.laboratory_name,
                       p.category_id,
                       c.category_name,
                       p.uom_id,
                       uom.unit_name
                  FROM products p
                       LEFT  JOIN laboratories l   ON l.id  = p.laboratory_id
                       LEFT  JOIN categories c     ON c.id  = p.category_id
                       INNER JOIN unit_of_measurement uom ON uom.id = p.uom_id
                 WHERE p.state
                   AND (@NameParam = '' OR p.product_name ILIKE @NameParam)
                 ORDER BY p.product_name
                 LIMIT @PageSize OFFSET @Offset;
            ";

            using var multi = await db.QueryMultipleAsync(sql, new { NameParam = nameParam, PageSize = pageSize, Offset = offset });
            int total = await multi.ReadSingleAsync<int>();
            var items = (await multi.ReadAsync<ProductResponse>()).ToList();
            return (items, total);
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }
    }

    public async Task<ProductResponse> GetProduct(Guid Id)
    {
        ProductResponse product = new();
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            string sqlQuery = @"
                     SELECT p.id,
                              p.product_code,
                              p.product_name,
                              p.description,
                              p.sale_price,
                              p.is_active,
                              p.current_stock,
                              p.min_reorder_quantity,
                              p.bar_code,
                              p.laboratory_id,
                              p.available_in_pos,
                              l.laboratory_name,
                              p.category_id,
                              c.category_name,
                              p.uom_id,
                              p.tracking_mode,
                              uom.unit_name
                         FROM products p
                              LEFT  JOIN laboratories l ON p.laboratory_id = l.id
                              LEFT JOIN categories c ON p.category_id = c.id
                              INNER JOIN unit_of_measurement uom ON uom.id = p.uom_id
                        WHERE p.state
                          AND p.id = @Id;
                ";
            var result = await db.QueryAsync<ProductResponse>(sqlQuery, new { id = Id });
            if (result!.ToList().Count > 0)
            {
                product = result!.ToList().First();
            }
            else
            {
                throw new CustomException("No existe el producto, de acuerdo a los parametros ingresados ", Common.Utilities.MessageTypes.Info);
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex, ex.messageType); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }
        return product;
    }

    public async Task<ProductStockPriceResponse> GetProductStockPrice(Guid Id)
    {
        ProductStockPriceResponse product = new();
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            string sqlQuery = @"
                     SELECT p.id,
                              p.sale_price,
                              p.current_stock
                         FROM products p
                        WHERE p.state
                          AND p.id = @Id;
                ";
            var result = await db.QueryAsync<ProductStockPriceResponse>(sqlQuery, new { id = Id });
            if (result!.ToList().Count > 0)
            {
                product = result!.ToList().First();
            }
            else
            {
                throw new CustomException("No existe el producto, de acuerdo a los parametros ingresados ", Common.Utilities.MessageTypes.Info);
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex, ex.messageType); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }
        return product;
    }

    public async Task<int> BulkUpdateProducts(List<ProductBulkUpdateRequest> items, int modifiedBy)
    {
        using var db = _DbContext.CreateConnection;
        int totalRows = 0;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                DateTime now = DateTime.Now;
                string sqlQuery = @"
                    UPDATE products
                       SET product_name       = @ProductName,
                           sale_price         = @SalePrice,
                           min_reorder_quantity = @MinReorderQuantity,
                           available_in_pos   = @AvailableInPos,
                           is_active          = @IsActive,
                           bar_code           = @BarCode,
                           modified_by        = @ModifiedBy,
                           modified           = @Modified
                     WHERE id    = @Id
                       AND state = true;
                ";

                foreach (var item in items)
                {
                    totalRows += await db.ExecuteAsync(sqlQuery, new
                    {
                        item.ProductName,
                        item.SalePrice,
                        item.MinReorderQuantity,
                        item.AvailableInPos,
                        item.IsActive,
                        item.BarCode,
                        ModifiedBy = modifiedBy,
                        Modified = now,
                        Id = Guid.Parse(item.Id)
                    }, transaction);
                }

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
        return totalRows;
    }

    /// <summary>
    /// Pasa el producto a seguimiento por lotes o por números de serie. La
    /// decisión de qué hacer con el stock que ya tenía vive en la función de
    /// base: queda como existencia sin identificar, que FEFO consume primero.
    /// </summary>
    /// <param name="modo">'lot' o 'serial'.</param>
    public async Task ActivateTracking(Guid id, string modo)
    {
        // El nombre de función no puede ir como parámetro, y esto NO se arma con
        // el texto que llegó del cliente: se elige entre dos constantes.
        var funcion = modo == "serial" ? "fn_activar_series" : "fn_activar_lotes";

        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            await db.ExecuteAsync($"SELECT {funcion}(@Id);", new { Id = id });
        }
        // El RAISE EXCEPTION de la función (producto inexistente, o ya con el
        // otro modo) lo traduce ExceptionHandler: su texto ya está escrito para
        // el usuario, así que no hace falta interceptarlo acá.
        catch (CustomException ex) { throw new CustomException(ex.Message, ex, ex.messageType); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally { db.Close(); }
    }

}
