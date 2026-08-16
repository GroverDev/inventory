using System.Data;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;

namespace Inventory.Infrastructure;

public class PurchaseDetailRepository : IPurchaseDetailRepository
{
    public async Task<bool> CreatePurchaseDetail(PurchaseDetail detail, IDbConnection db, IDbTransaction transaction)
    {
        bool ok;
        try
        {
            detail.Id = Guid.NewGuid();
            string sqlQuery = @"
                    INSERT INTO purchases_detail
                        (id,   purchase_id, product_id, order_unit_price, ordered_quantity, order_final_price, state, created_by, created, modified_by, modified, delivery_unit_price, delivered_quantity, delivery_final_price, purchase_status_id)
                    VALUES
                        (@Id, @PurchaseId, @ProductId, @OrderUnitPrice, @OrderedQuantity, @OrderFinalPrice, @State, @CreatedBy,  @Created, @ModifiedBy, @Modified, @DeliveryUnitPrice, @DeliveredQuantity, @DeliveryFinalPrice, @PurchaseStatusId);
                ";

            var result = await db.ExecuteAsync(sqlQuery, detail, transaction: transaction);
            ok = true;
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }

        return ok;
    }

    public async Task<bool> UpdatePurchaseDetail(PurchaseDetail detail, IDbConnection db, IDbTransaction transaction)
    {
        bool ok;
        try
        {
            string sqlQuery = @"
                    UPDATE purchases_detail
                       SET modified_by = @ModifiedBy,
                           modified = @Modified,
                           order_unit_price = @OrderUnitPrice,
                           ordered_quantity = @OrderedQuantity,
                           order_final_price = @OrderFinalPrice,
                           purchase_status_id = @PurchaseStatusId
                    WHERE id = @Id and product_id = @ProductId and purchase_id = @PurchaseId;
                ";

            var result = await db.ExecuteAsync(sqlQuery, detail, transaction);
            ok = true;
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }

        return ok;
    }

    /// <summary>Una entrada de mercadería sobre una existencia concreta.</summary>
    private readonly record struct Entrada(
        Guid StockItemId, decimal StockBefore, decimal StockAfter, int Cantidad);

    /// <summary>
    /// Da entrada al stock según cómo se identifique el producto, y devuelve una
    /// fila por existencia afectada.
    /// </summary>
    /// <remarks>
    /// Con lotes y sin seguimiento hay una sola existencia; con series hay una
    /// por unidad, porque cada número identifica una unidad física distinta.
    /// Los tres caminos terminan en <c>fn_mover_stock</c>, así que la caché de
    /// <c>products.current_stock</c> queda alineada en todos los casos.
    /// </remarks>
    private static async Task<List<Entrada>> RegistrarEntradas(
        string modo, PurchaseDeliveryDetail detail, IDbConnection db, IDbTransaction transaction)
    {
        const string sqlEntrada = "SELECT stock_item_id, stock_before, stock_after FROM ";

        if (modo == "lot")
        {
            if (string.IsNullOrWhiteSpace(detail.LotCode))
                throw new CustomException(
                    "El producto usa seguimiento por lotes: hay que indicar el lote recibido.");

            // Los dos casts son obligatorios, no decorativos: la función se
            // declara (uuid, numeric, varchar, date, integer) y Npgsql manda el
            // DateTime como `timestamp` y el string como `text`. PostgreSQL no
            // convierte timestamp→date al resolver la función, así que sin el
            // cast falla con "function ... does not exist".
            var lote = await db.QueryFirstAsync<(Guid StockItemId, decimal StockBefore, decimal StockAfter)>(
                sqlEntrada + "fn_recibir_lote(@ProductId, @Cantidad, @Lote::varchar, @Vence::date, @UserId)",
                new
                {
                    detail.ProductId,
                    Cantidad = (decimal)detail.DeliveryQuantity,
                    Lote = detail.LotCode,
                    Vence = detail.ExpiryDate,
                    UserId = detail.CreatedBy
                }, transaction);

            return [new Entrada(lote.StockItemId, lote.StockBefore, lote.StockAfter, detail.DeliveryQuantity)];
        }

        if (modo == "serial")
        {
            var series = (detail.SerialNumbers ?? [])
                .Select(s => (s ?? "").Trim())
                .Where(s => s.Length > 0)
                .ToList();

            // Una unidad, un número: recibir 5 aparatos exige 5 números. Si no
            // coinciden, alguien se saltó una etiqueta o tecleó de más, y dejarlo
            // pasar significaría stock que nadie puede identificar después.
            if (series.Count != detail.DeliveryQuantity)
                throw new CustomException(
                    $"El producto se identifica por número de serie: hay que indicar " +
                    $"{detail.DeliveryQuantity} número(s) y llegaron {series.Count}.");

            if (series.Distinct(StringComparer.OrdinalIgnoreCase).Count() != series.Count)
                throw new CustomException(
                    "Hay números de serie repetidos en esta recepción: cada uno identifica una unidad distinta.");

            var entradas = new List<Entrada>(series.Count);
            foreach (var serie in series)
            {
                var fila = await db.QueryFirstAsync<(Guid StockItemId, decimal StockBefore, decimal StockAfter)>(
                    sqlEntrada + "fn_recibir_serie(@ProductId, @Serie::varchar, @Vence::date, @UserId)",
                    new
                    {
                        detail.ProductId,
                        Serie = serie,
                        Vence = detail.ExpiryDate,
                        UserId = detail.CreatedBy
                    }, transaction);

                entradas.Add(new Entrada(fila.StockItemId, fila.StockBefore, fila.StockAfter, 1));
            }
            return entradas;
        }

        var simple = await db.QueryFirstAsync<(Guid StockItemId, decimal StockBefore, decimal StockAfter)>(
            sqlEntrada + "fn_mover_stock(@ProductId, @Delta, @UserId)",
            new { detail.ProductId, Delta = (decimal)detail.DeliveryQuantity, UserId = detail.CreatedBy },
            transaction);

        return [new Entrada(simple.StockItemId, simple.StockBefore, simple.StockAfter, detail.DeliveryQuantity)];
    }

    /// <summary>
    /// Registra una línea de recepción: graba el hecho, mueve el stock, deja el
    /// movimiento auditable y actualiza el acumulado de la línea del pedido.
    /// Todo dentro de la transacción que abre <see cref="PurchaseRepository.ReceiveOrders"/>.
    /// </summary>
    public async Task<bool> ReceiveOrdersDetail(Guid purchaseId, PurchaseDeliveryDetail detail, IDbConnection db, IDbTransaction transaction)
    {
        bool ok;
        try
        {
            detail.Id = Guid.NewGuid();
            string sqlQuery = @"
                    INSERT INTO purchases_delivery_detail
                            (id, purchase_delivery_id, product_id, ordered_quantity, delivery_quantity, unit_price, state, created_by, created, modified_by, modified)
                    VALUES(@Id, @PurchaseDeliveryId, @ProductId, @OrderedQuantity, @DeliveryQuantity, @UnitPrice, @State, @CreatedBy, @Created, @ModifiedBy , @Modified);
                ";

            await db.ExecuteAsync(sqlQuery, detail, transaction);

            // La recepción es el único momento en que el lote o la serie entran al
            // sistema: es cuando la caja física llega con su etiqueta. Después ya
            // no hay forma de saber de qué lote era ni qué unidad se recibió.
            string modo = await db.ExecuteScalarAsync<string>(
                "SELECT tracking_mode FROM products WHERE id = @ProductId",
                new { detail.ProductId }, transaction) ?? "none";

            // Cada entrada mueve una existencia distinta. Con lotes o sin
            // seguimiento hay una sola; con series hay una por unidad, y el libro
            // mayor guarda un movimiento por cada una: es lo que después permite
            // decir qué unidad concreta entró y cuándo.
            var entradas = await RegistrarEntradas(modo, detail, db, transaction);

            string movSql = @"
                INSERT INTO stock_movements
                       (id, product_id, stock_item_id, movement_type, quantity, stock_before, stock_after,
                        reason, observation, reference_id, reference_type,
                        state, created_by, created, modified_by, modified)
                VALUES (@Id, @ProductId, @StockItemId, @MovementType, @Quantity, @StockBefore, @StockAfter,
                        @Reason, @Observation, @ReferenceId, @ReferenceType,
                        @State, @CreatedBy, @Created, @ModifiedBy, @Modified);
            ";

            foreach (var entrada in entradas)
            {
                await db.ExecuteAsync(movSql, new StockMovement
                {
                    Id = Guid.NewGuid(),
                    ProductId = detail.ProductId,
                    StockItemId = entrada.StockItemId,
                    MovementType = "COMPRA",
                    Quantity = entrada.Cantidad,
                    StockBefore = (int)entrada.StockBefore,
                    StockAfter = (int)entrada.StockAfter,
                    ReferenceId = detail.PurchaseDeliveryId,
                    ReferenceType = "PURCHASE",
                    State = true,
                    CreatedBy = detail.CreatedBy,
                    ModifiedBy = detail.CreatedBy,
                    Created = DateTime.Now,
                    Modified = DateTime.Now,
                }, transaction);
            }

            // Caché denormalizado sobre la línea del pedido. La verdad sigue siendo
            // el log de recepciones; esto solo evita recalcularlo en cada consulta.
            string cacheSql = @"
                UPDATE purchases_detail
                   SET delivered_quantity   = delivered_quantity + @DeliveryQuantity,
                       delivery_unit_price  = @UnitPrice,
                       delivery_final_price = delivery_final_price + (@DeliveryQuantity * @UnitPrice),
                       modified_by          = @ModifiedBy,
                       modified             = @Modified
                 WHERE purchase_id = @PurchaseId
                   AND product_id  = @ProductId
                   AND state;
            ";
            await db.ExecuteAsync(cacheSql, new
            {
                detail.DeliveryQuantity,
                detail.UnitPrice,
                detail.ModifiedBy,
                detail.Modified,
                PurchaseId = purchaseId,
                detail.ProductId
            }, transaction);

            ok = true;
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }

        return ok;
    }
}
