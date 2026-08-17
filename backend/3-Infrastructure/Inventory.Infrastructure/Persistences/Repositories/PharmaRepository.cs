using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;

namespace Inventory.Infrastructure;

/// <summary>
/// Datos del rubro farmacia. Vive aparte del repositorio de productos a
/// propósito: el núcleo no tiene que saber que este rubro existe.
/// </summary>
public class PharmaRepository(InventoryDbContext _DbContext) : IPharmaRepository
{
    public async Task<List<PharmaCatalogItem>> GetForms()
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            var r = await db.QueryAsync<PharmaCatalogItem>(
                "SELECT id, form_name AS Name FROM pharma_forms WHERE state ORDER BY form_name");
            return [.. r];
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally { db.Close(); }
    }

    public async Task<List<PharmaCatalogItem>> GetRoutes()
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            var r = await db.QueryAsync<PharmaCatalogItem>(
                "SELECT id, route_name AS Name FROM pharma_routes WHERE state ORDER BY route_name");
            return [.. r];
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally { db.Close(); }
    }

    public async Task<List<PharmaSubstance>> SearchSubstances(string nombre)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            var r = await db.QueryAsync<PharmaSubstance>(@"
                SELECT id, substance_name, therapeutic_group
                  FROM pharma_substances
                 WHERE state AND substance_name ILIKE @Nombre
                 ORDER BY substance_name
                 LIMIT 50",
                new { Nombre = "%" + nombre + "%" });
            return [.. r];
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally { db.Close(); }
    }

    public async Task<ProductPharma?> GetByProduct(Guid productId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();

            var datos = await db.QueryFirstOrDefaultAsync<ProductPharma>(@"
                SELECT pp.product_id, pp.form_id, pp.route_id,
                       COALESCE(f.form_name, '')  AS FormName,
                       COALESCE(r.route_name, '') AS RouteName,
                       pp.presentation, pp.dosage_reference, pp.product_type,
                       pp.sanitary_registry, pp.sanitary_registry_expiry
                  FROM product_pharma pp
                       LEFT JOIN pharma_forms  f ON f.id = pp.form_id
                       LEFT JOIN pharma_routes r ON r.id = pp.route_id
                 WHERE pp.product_id = @ProductId AND pp.state",
                new { ProductId = productId });

            // Los componentes se devuelven aunque no haya ficha farmacéutica: se
            // puede cargar la composición sin haber completado forma ni vía.
            var componentes = await db.QueryAsync<ProductComponent>(@"
                SELECT pc.id, pc.product_id, pc.substance_id, s.substance_name,
                       pc.concentration_value, pc.concentration_unit,
                       pc.is_active_ingredient, pc.show_order
                  FROM product_components pc
                       INNER JOIN pharma_substances s ON s.id = pc.substance_id
                 WHERE pc.product_id = @ProductId AND pc.state
                 ORDER BY pc.is_active_ingredient DESC, pc.show_order, s.substance_name",
                new { ProductId = productId });

            var lista = componentes.ToList();
            if (datos is null && lista.Count == 0) return null;

            datos ??= new ProductPharma { ProductId = productId };
            datos.Components = lista;
            return datos;
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally { db.Close(); }
    }

    /// <summary>
    /// Guarda la ficha y su composición en una transacción.
    /// </summary>
    /// <remarks>
    /// Los componentes se reemplazan en bloque en vez de calcular altas, bajas y
    /// modificaciones: son pocos por producto, llegan siempre completos desde la
    /// pantalla, y el diff sería más código con más formas de equivocarse.
    /// </remarks>
    public async Task Save(Guid productId, ProductPharma datos, List<(Guid? SubstanceId, string SubstanceName, decimal? Value, string? Unit, bool EsActivo)> componentes, int userId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                await db.ExecuteAsync(@"
                    INSERT INTO product_pharma
                        (product_id, form_id, route_id, presentation, dosage_reference,
                         product_type, sanitary_registry, sanitary_registry_expiry,
                         created_by, modified_by)
                    VALUES (@ProductId, @FormId, @RouteId, @Presentation, @DosageReference,
                            @ProductType, @SanitaryRegistry, @SanitaryRegistryExpiry,
                            @UserId, @UserId)
                    ON CONFLICT (product_id) DO UPDATE SET
                        form_id                  = EXCLUDED.form_id,
                        route_id                 = EXCLUDED.route_id,
                        presentation             = EXCLUDED.presentation,
                        dosage_reference         = EXCLUDED.dosage_reference,
                        product_type             = EXCLUDED.product_type,
                        sanitary_registry        = EXCLUDED.sanitary_registry,
                        sanitary_registry_expiry = EXCLUDED.sanitary_registry_expiry,
                        state                    = true,
                        modified_by              = EXCLUDED.modified_by,
                        modified                 = now()",
                    new
                    {
                        ProductId = productId,
                        datos.FormId,
                        datos.RouteId,
                        datos.Presentation,
                        datos.DosageReference,
                        datos.ProductType,
                        datos.SanitaryRegistry,
                        datos.SanitaryRegistryExpiry,
                        UserId = userId
                    }, transaction);

                await db.ExecuteAsync(
                    "DELETE FROM product_components WHERE product_id = @ProductId",
                    new { ProductId = productId }, transaction);

                int orden = 0;
                foreach (var c in componentes)
                {
                    var sustanciaId = c.SubstanceId;

                    // Alta al vuelo: si la sustancia llega por nombre, se busca y
                    // se crea si no existe. Es lo que permite cargar un producto
                    // sin salir a otra pantalla a dar de alta el principio activo.
                    if (sustanciaId is null || sustanciaId == Guid.Empty)
                    {
                        var nombre = (c.SubstanceName ?? "").Trim();
                        if (nombre.Length == 0)
                            throw new CustomException("Cada componente necesita una sustancia.", MessageTypes.Warning);

                        sustanciaId = await db.QueryFirstOrDefaultAsync<Guid?>(
                            "SELECT id FROM pharma_substances WHERE upper(trim(substance_name)) = upper(@Nombre) AND state",
                            new { Nombre = nombre }, transaction);

                        sustanciaId ??= await db.ExecuteScalarAsync<Guid>(@"
                            INSERT INTO pharma_substances (substance_name, created_by, modified_by)
                            VALUES (@Nombre, @UserId, @UserId) RETURNING id",
                            new { Nombre = nombre, UserId = userId }, transaction);
                    }

                    await db.ExecuteAsync(@"
                        INSERT INTO product_components
                            (product_id, substance_id, concentration_value, concentration_unit,
                             is_active_ingredient, show_order, created_by, modified_by)
                        VALUES (@ProductId, @SubstanceId, @Value, @Unit, @EsActivo, @Orden, @UserId, @UserId)",
                        new
                        {
                            ProductId = productId,
                            SubstanceId = sustanciaId,
                            c.Value,
                            c.Unit,
                            c.EsActivo,
                            Orden = orden++,
                            UserId = userId
                        }, transaction);
                }

                transaction.Commit();
            }
            catch (CustomException ex) { transaction.Rollback(); throw new CustomException(ex.Message, ex, ex.messageType); }
            catch { transaction.Rollback(); throw; }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex, ex.messageType); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally { db.Close(); }
    }

    /// <summary>
    /// Prospecto del producto. <c>null</c> si no tiene: la mayoría no lo va a
    /// tener, y por eso vive en una tabla aparte y se pide por separado.
    /// </summary>
    public async Task<string?> GetLeaflet(Guid productId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            return await db.QueryFirstOrDefaultAsync<string?>(
                "SELECT content FROM product_leaflet WHERE product_id = @ProductId AND state",
                new { ProductId = productId });
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally { db.Close(); }
    }

    /// <summary>
    /// Guarda el prospecto, o lo borra si llega vacío.
    /// </summary>
    /// <remarks>
    /// Vaciar el campo es la forma natural de decir "este producto no tiene
    /// prospecto"; dejar una fila con texto en blanco haría que la pantalla
    /// mostrara una sección vacía en vez de ninguna.
    /// </remarks>
    public async Task SaveLeaflet(Guid productId, string? contenido, int userId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();

            if (string.IsNullOrWhiteSpace(contenido))
            {
                await db.ExecuteAsync("DELETE FROM product_leaflet WHERE product_id = @ProductId",
                    new { ProductId = productId });
                return;
            }

            await db.ExecuteAsync(@"
                INSERT INTO product_leaflet (product_id, content, created_by, modified_by)
                VALUES (@ProductId, @Contenido, @UserId, @UserId)
                ON CONFLICT (product_id) DO UPDATE SET
                    content     = EXCLUDED.content,
                    state       = true,
                    modified_by = EXCLUDED.modified_by,
                    modified    = now()",
                new { ProductId = productId, Contenido = contenido.Trim(), UserId = userId });
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally { db.Close(); }
    }

    /// <summary>
    /// Alternativas definidas a mano por la farmacia.
    /// </summary>
    /// <remarks>
    /// Estas NO se deducen: son la alternativa comercial, la más económica, la
    /// que el cliente suele preferir. Pueden tener otro principio activo, y por
    /// eso quien vende tiene que verlas distintas de un equivalente real.
    /// </remarks>
    public async Task<List<ProductEquivalentResponse>> GetManualAlternatives(Guid productId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            var r = await db.QueryAsync<ProductEquivalentResponse>(@"
                SELECT p.id            AS ProductId,
                       p.product_name,
                       p.sale_price,
                       p.current_stock,
                       COALESCE(pp.product_type, '') AS ProductType,
                       COALESCE(pp.presentation, '') AS Presentation,
                       true            AS IsManual,
                       COALESCE(pa.reason, '')       AS Reason
                  FROM product_alternatives pa
                       JOIN products p ON p.id = pa.alternative_id AND p.state AND p.is_active
                       LEFT JOIN product_pharma pp ON pp.product_id = p.id
                 WHERE pa.product_id = @ProductId AND pa.state
                 ORDER BY pa.show_order, p.sale_price",
                new { ProductId = productId });
            return [.. r];
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally { db.Close(); }
    }

    public async Task AddAlternative(Guid productId, Guid alternativeId, string? motivo, int userId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            // ON CONFLICT y no un chequeo previo: entre la consulta y el insert
            // otro cajero pudo haber cargado la misma, y el índice único es lo
            // único que decide de verdad.
            await db.ExecuteAsync(@"
                INSERT INTO product_alternatives
                    (product_id, alternative_id, reason, created_by, modified_by)
                VALUES (@ProductId, @AlternativeId, @Motivo, @UserId, @UserId)
                ON CONFLICT (tenant_id, product_id, alternative_id) WHERE state
                DO UPDATE SET reason = EXCLUDED.reason, modified_by = EXCLUDED.modified_by, modified = now()",
                new { ProductId = productId, AlternativeId = alternativeId, Motivo = motivo, UserId = userId });
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally { db.Close(); }
    }

    public async Task RemoveAlternative(Guid productId, Guid alternativeId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            await db.ExecuteAsync(
                "DELETE FROM product_alternatives WHERE product_id = @ProductId AND alternative_id = @AlternativeId",
                new { ProductId = productId, AlternativeId = alternativeId });
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally { db.Close(); }
    }

    /// <summary>
    /// Equivalentes por composición: mismos principios activos, en las mismas
    /// concentraciones.
    /// </summary>
    /// <remarks>
    /// No se carga nada a mano — se deduce de la composición. Es el motivo por el
    /// que el principio activo se modeló como relación y no como texto: con texto
    /// libre esta consulta sería imposible.
    /// </remarks>
    public async Task<List<ProductEquivalentResponse>> GetEquivalents(Guid productId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            var r = await db.QueryAsync<ProductEquivalentResponse>(@"
                WITH composicion AS (
                    SELECT pc.product_id,
                           array_agg((pc.substance_id, pc.concentration_value, pc.concentration_unit)
                                     ORDER BY pc.substance_id) AS formula
                      FROM product_components pc
                     WHERE pc.state AND pc.is_active_ingredient
                     GROUP BY pc.product_id
                )
                SELECT p.id            AS ProductId,
                       p.product_name,
                       p.sale_price,
                       p.current_stock,
                       COALESCE(pp.product_type, '') AS ProductType,
                       COALESCE(pp.presentation, '') AS Presentation,
                       false           AS IsManual,
                       ''              AS Reason
                  FROM composicion base
                       JOIN composicion otro ON otro.formula = base.formula
                                            AND otro.product_id <> base.product_id
                       JOIN products p ON p.id = otro.product_id AND p.state AND p.is_active
                       LEFT JOIN product_pharma pp ON pp.product_id = p.id
                 WHERE base.product_id = @ProductId
                 ORDER BY p.sale_price",
                new { ProductId = productId });
            return [.. r];
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally { db.Close(); }
    }
}
