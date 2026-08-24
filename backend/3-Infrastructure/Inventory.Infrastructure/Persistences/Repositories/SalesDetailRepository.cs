using System.Data;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;

namespace Inventory.Infrastructure;

public class SalesDetailRepository : ISalesDetailRepository
{
    /// <summary>Una porción de la línea vendida, salida de una existencia concreta.</summary>
    private readonly record struct Asignacion(Guid StockItemId, decimal Cantidad);

    /// <summary>
    /// Graba una línea de venta y descuenta el stock del que sale.
    /// </summary>
    /// <remarks>
    /// El reparto lo decide <c>fn_asignar_fefo</c>, que ordena por vencimiento más
    /// próximo. Un producto sin seguimiento devuelve una sola asignación —su
    /// existencia implícita—, así que no hay dos caminos: el caso simple es el caso
    /// general con una sola porción.
    /// <para>
    /// Cuando la cantidad abarca varios lotes se graba una línea por lote. En una
    /// farmacia eso es deseable: es lo que permite saber a quién se le vendió cuál
    /// ante un retiro del laboratorio. Los importes se prorratean por cantidad, y
    /// el redondeo sobrante va a la última porción para que la suma de las líneas
    /// coincida exactamente con el total que envió el punto de venta.
    /// </para>
    /// </remarks>
    public async Task<bool> CreateSaleDetail(SaleDetail detail, IDbConnection db, IDbTransaction transaction)
    {
        try
        {
            // Con números de serie elegidos en el mostrador manda el mostrador:
            // FEFO sirve para lo intercambiable, pero una unidad serializada se
            // entrega en mano y la garantía queda atada a ESE número, no al que
            // vencía antes. Sin series indicadas se sigue por FEFO como siempre.
            var asignaciones = await ResolverSeries(detail, db, transaction)
                ?? (await db.QueryAsync<Asignacion>(
                    "SELECT stock_item_id AS StockItemId, cantidad AS Cantidad FROM fn_asignar_fefo(@ProductId, @Cantidad)",
                    new { detail.ProductId, Cantidad = (decimal)detail.Quantity },
                    transaction)).ToList();

            if (asignaciones.Count == 0)
                throw new CustomException("No se pudo determinar de qué existencia sale la venta.");

            decimal subtotalPendiente  = detail.LineSubtotal;
            decimal descuentoPendiente = detail.LineTotalDiscounts;
            decimal totalPendiente     = detail.LineTotal;
            int     cantidadPendiente  = detail.Quantity;

            for (int i = 0; i < asignaciones.Count; i++)
            {
                bool esUltima = i == asignaciones.Count - 1;
                int  cantidad = esUltima ? cantidadPendiente : (int)asignaciones[i].Cantidad;

                // La última porción se lleva lo que quede, así se evita que el
                // redondeo haga que las líneas no sumen el total de la venta.
                decimal subtotal  = esUltima ? subtotalPendiente  : Prorratear(detail.LineSubtotal, cantidad, detail.Quantity);
                decimal descuento = esUltima ? descuentoPendiente : Prorratear(detail.LineTotalDiscounts, cantidad, detail.Quantity);
                decimal total     = esUltima ? totalPendiente     : Prorratear(detail.LineTotal, cantidad, detail.Quantity);

                subtotalPendiente  -= subtotal;
                descuentoPendiente -= descuento;
                totalPendiente     -= total;
                cantidadPendiente  -= cantidad;

                var porcion = new SaleDetail
                {
                    Id                 = Guid.NewGuid(),
                    SaleId             = detail.SaleId,
                    ProductId          = detail.ProductId,
                    StockItemId        = asignaciones[i].StockItemId,
                    Quantity           = cantidad,
                    UnitPrice          = detail.UnitPrice,
                    LineSubtotal       = subtotal,
                    LineTotalDiscounts = descuento,
                    LineTotal          = total,
                    DiscountId         = detail.DiscountId,
                    State              = detail.State,
                    CreatedBy          = detail.CreatedBy,
                    ModifiedBy         = detail.ModifiedBy,
                    Created            = detail.Created,
                    Modified           = detail.Modified,
                };

                await GrabarPorcion(porcion, db, transaction);
            }

            return true;
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
    }

    /// <summary>
    /// Convierte las series elegidas en el mostrador en asignaciones de stock.
    /// Devuelve <c>null</c> cuando no se eligió ninguna, que es la señal de
    /// seguir por FEFO.
    /// </summary>
    /// <remarks>
    /// Valida contra la base y no contra lo que mandó el cliente: que cada serie
    /// exista, sea de ESTE producto y siga disponible. Dos cajas cobrando a la
    /// vez pueden elegir la misma unidad, y la segunda tiene que enterarse acá y
    /// no dejar el inventario en negativo.
    /// </remarks>
    private static async Task<List<Asignacion>?> ResolverSeries(
        SaleDetail detail, IDbConnection db, IDbTransaction transaction)
    {
        var series = (detail.SerialNumbers ?? [])
            .Select(s => (s ?? "").Trim())
            .Where(s => s.Length > 0)
            .ToList();

        if (series.Count == 0) return null;

        if (series.Count != detail.Quantity)
            throw new CustomException(
                $"Se indicaron {series.Count} número(s) de serie para una cantidad de {detail.Quantity}: " +
                "cada unidad se entrega con su número.");

        if (series.Distinct(StringComparer.OrdinalIgnoreCase).Count() != series.Count)
            throw new CustomException(
                "Hay números de serie repetidos: cada uno identifica una unidad distinta.");

        var encontradas = (await db.QueryAsync<(Guid Id, string Serie)>(
            @"SELECT id, serial_number
                FROM stock_items
               WHERE product_id = @ProductId
                 AND state
                 AND quantity > 0
                 AND upper(trim(serial_number)) = ANY(@Series)",
            new
            {
                detail.ProductId,
                Series = series.Select(s => s.ToUpperInvariant()).ToArray()
            },
            transaction)).ToList();

        if (encontradas.Count != series.Count)
        {
            var faltantes = series
                .Where(s => !encontradas.Any(e =>
                    string.Equals(e.Serie?.Trim(), s, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            throw new CustomException(
                $"No hay stock disponible de la(s) serie(s): {string.Join(", ", faltantes)}. " +
                "Puede que ya se hayan vendido.");
        }

        return [.. encontradas.Select(e => new Asignacion(e.Id, 1m))];
    }

    /// <summary>Reparte un importe en proporción a la cantidad, a dos decimales.</summary>
    private static decimal Prorratear(decimal importe, int parte, int total)
        => total == 0 ? 0 : Math.Round(importe * parte / total, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Graba una porción: la línea, su descuento si lo tiene, el descuento de stock
    /// sobre la existencia concreta y el movimiento que lo documenta.
    /// </summary>
    private static async Task GrabarPorcion(SaleDetail porcion, IDbConnection db, IDbTransaction transaction)
    {
        // Mover el stock va primero: sales_detail.stock_item_id es NOT NULL, así
        // que la línea no se puede grabar sin saber de qué existencia sale.
        var mov = await db.QueryFirstAsync<(Guid StockItemId, decimal StockBefore, decimal StockAfter)>(
            "SELECT stock_item_id, stock_before, stock_after FROM fn_mover_stock(@ProductId, @Delta, @UserId, @Item)",
            new
            {
                porcion.ProductId,
                Delta = -(decimal)porcion.Quantity,
                UserId = porcion.CreatedBy,
                Item = porcion.StockItemId
            },
            transaction);

        await db.ExecuteAsync(@"
            INSERT INTO sales_detail
                   (id, sale_id, product_id, stock_item_id, quantity, unit_price,
                    line_subtotal, line_total_discounts, line_total, discount_id,
                    state, created_by, created, modified_by, modified)
            VALUES (@Id, @SaleId, @ProductId, @StockItemId, @Quantity, @UnitPrice,
                    @LineSubtotal, @LineTotalDiscounts, @LineTotal, @DiscountId,
                    @State, @CreatedBy, @Created, @ModifiedBy, @Modified);",
            porcion, transaction);

        if (porcion.DiscountId.HasValue && porcion.DiscountId != Guid.Empty)
        {
            await db.ExecuteAsync(@"
                INSERT INTO sale_detail_discounts
                       (id, sale_detail_id, discount_id, applied_amount,
                        state, created_by, created, modified_by, modified)
                VALUES (@Id, @SaleDetailId, @DiscountId, @AppliedAmount,
                        true, @CreatedBy, @Created, @CreatedBy, @Created);",
                new
                {
                    Id = Guid.NewGuid(),
                    SaleDetailId = porcion.Id,
                    porcion.DiscountId,
                    AppliedAmount = porcion.LineTotalDiscounts,
                    porcion.CreatedBy,
                    Created = DateTime.UtcNow
                }, transaction);
        }

        await db.ExecuteAsync(@"
            INSERT INTO stock_movements
                   (id, product_id, stock_item_id, movement_type, quantity, stock_before, stock_after,
                    reason, observation, reference_id, reference_type,
                    state, created_by, created, modified_by, modified)
            VALUES (@Id, @ProductId, @StockItemId, @MovementType, @Quantity, @StockBefore, @StockAfter,
                    @Reason, @Observation, @ReferenceId, @ReferenceType,
                    @State, @CreatedBy, @Created, @ModifiedBy, @Modified);",
            new StockMovement
            {
                Id            = Guid.NewGuid(),
                ProductId     = porcion.ProductId,
                StockItemId   = mov.StockItemId,
                MovementType  = "VENTA",
                Quantity      = -porcion.Quantity,
                StockBefore   = (int)mov.StockBefore,
                StockAfter    = (int)mov.StockAfter,
                ReferenceId   = porcion.SaleId,
                ReferenceType = "SALE",
                State         = true,
                CreatedBy     = porcion.CreatedBy,
                ModifiedBy    = porcion.CreatedBy,
                Created       = DateTime.UtcNow,
                Modified      = DateTime.UtcNow,
            }, transaction);
    }

    public async Task<List<SaleProductDetailResponse>> GetSalesProductDetail(Guid idSale, IDbConnection db)
    {
        List<SaleProductDetailResponse> listDetails = [];
        try
        {
            // Se incluyen lote y vencimiento: con varias porciones de un mismo
            // producto es lo único que distingue una línea de otra en el detalle.
            string sqlQuery = @" SELECT sd.id,
                                        sd.sale_id,
                                        sd.product_id,
                                        p.product_name,
                                        si.lot_code,
                                        si.expiry_date,
                                        si.serial_number,
                                        sd.quantity,
                                        sd.unit_price,
                                        sd.line_subtotal,
                                        sd.line_total_discounts,
                                        sd.line_total,
                                        -- Precio unitario efectivamente cobrado: la linea ya
                                        -- con su descuento, menos la parte que le toca del
                                        -- descuento global, dividido por la cantidad. Es lo
                                        -- que se reembolsa al devolver, y lo que el POS
                                        -- muestra al previsualizar una devolucion.
                                        -- El neto sale de line_subtotal - descuentos y no de
                                        -- line_total, porque hay ventas viejas con line_total
                                        -- truncado a entero (10.50 guardado como 10).
                                        ROUND((sd.line_subtotal - COALESCE(sd.line_total_discounts, 0)
                                               - COALESCE(s.header_discount_amount, 0)
                                                 * (sd.line_subtotal - COALESCE(sd.line_total_discounts, 0))
                                                 / NULLIF(SUM(sd.line_subtotal - COALESCE(sd.line_total_discounts, 0)) OVER (), 0)
                                              ) / NULLIF(sd.quantity, 0), 2) AS EffectiveUnitPrice
                                    FROM sales_detail sd
                                         INNER JOIN products p ON p.id = sd.product_id
                                         INNER JOIN sales s ON s.id = sd.sale_id
                                         LEFT  JOIN stock_items si ON si.id = sd.stock_item_id
                                   WHERE sd.sale_id = @sale_id";
            var result = await db.QueryAsync<SaleProductDetailResponse>(sqlQuery, new { sale_id = idSale });
            listDetails = result.ToList();
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        return listDetails;
    }
}
