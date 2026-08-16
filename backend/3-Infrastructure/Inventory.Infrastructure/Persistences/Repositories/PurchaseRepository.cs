using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;
using Npgsql;

namespace Inventory.Infrastructure;

public class PurchaseRepository(IPurchaseDetailRepository _purchaseDetailRepository, InventoryDbContext _DbContext):IPurchaseRepository
{
    /// <summary>Nombre del índice único que hace idempotente la recepción.</summary>
    private const string OperationUidConstraint = "uq_purchases_delivery_operation_uid";

    /// <summary>
    /// Saldos por producto: lo ordenado contra la suma real del log de recepciones.
    ///
    /// Se agrupa por producto y no por línea porque el stock se mueve por producto:
    /// si una orden histórica repite el mismo producto en dos líneas, el pendiente
    /// sigue siendo uno solo. LATERAL evita el fan-out de un JOIN directo.
    /// </summary>
    private const string SqlLineBalances = @"
        SELECT ord.product_id,
               ord.product_name,
               ord.ordered_quantity,
               COALESCE(rec.received, 0) AS received_quantity
          FROM (
                SELECT pd.product_id,
                       MIN(p.product_name)      AS product_name,
                       SUM(pd.ordered_quantity) AS ordered_quantity
                  FROM purchases_detail pd
                       INNER JOIN products p ON p.id = pd.product_id
                 WHERE pd.purchase_id = @PurchaseId
                   AND pd.state
                 GROUP BY pd.product_id
               ) ord
               LEFT JOIN LATERAL (
                    SELECT SUM(pdd.delivery_quantity) AS received
                      FROM purchases_delivery pdl
                           INNER JOIN purchases_delivery_detail pdd
                                   ON pdd.purchase_delivery_id = pdl.id
                                  AND pdd.state
                     WHERE pdl.purchase_id = @PurchaseId
                       AND pdd.product_id  = ord.product_id
                       AND pdl.state
               ) rec ON TRUE;
    ";

    /// <summary>Detalle de la orden enriquecido con recibido y pendiente.</summary>
    private const string SqlPurchaseDetail = @"
        SELECT pd.id,
               pd.purchase_id,
               pd.product_id,
               p.product_name,
               pd.ordered_quantity,
               pd.order_unit_price,
               pd.order_final_price,
               pd.delivery_unit_price,
               pd.delivered_quantity,
               pd.delivery_final_price,
               pd.purchase_status_id,
               p.tracking_mode,
               COALESCE(rec.received, 0)                                    AS received_quantity,
               GREATEST(pd.ordered_quantity - COALESCE(rec.received, 0), 0) AS pending_quantity
          FROM purchases_detail pd
               INNER JOIN products p ON p.id = pd.product_id
               LEFT JOIN LATERAL (
                    SELECT SUM(pdd.delivery_quantity) AS received
                      FROM purchases_delivery pdl
                           INNER JOIN purchases_delivery_detail pdd
                                   ON pdd.purchase_delivery_id = pdl.id
                                  AND pdd.state
                     WHERE pdl.purchase_id = pd.purchase_id
                       AND pdd.product_id  = pd.product_id
                       AND pdl.state
               ) rec ON TRUE
         WHERE pd.purchase_id = @PurchaseId
           AND pd.state;
    ";

    public async Task<bool> CreatePurchase(Purchase purchase)
    {
        using var db = _DbContext.CreateConnection;
        bool ok;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                purchase.Id = Guid.NewGuid();
                string sqlQuery = @"
                        INSERT INTO purchases
                             (id,  purchase_date,  total, is_active,  state, created_by,  created,  modified_by, modified, provider_id, estimated_delivery_date, purchase_status_id)
                       VALUES(@Id, @PurchaseDate, @Total, @IsActive, @State, @CreatedBy,  @Created, @ModifiedBy, @Modified, @ProviderId, @EstimatedDeliveryDate, @PurchaseStatusId)      ";

                purchase.PurchaseStatusId = (int)Domain.Enums.PurchaseStatusEnum.REQUESTED;
                var result = await db.ExecuteAsync(sqlQuery, purchase, transaction);

                purchase.Detail.ForEach(x => { x.PurchaseId = purchase.Id; x.PurchaseStatusId = purchase.PurchaseStatusId; });

                foreach (var detail in purchase.Detail)
                {
                    detail.State = true;
                    await _purchaseDetailRepository.CreatePurchaseDetail(detail, db, transaction);
                }
                transaction.Commit();
                ok = true;
            }
            catch (CustomException ex)
            {
                transaction.Rollback();
                throw new CustomException(ex.Message, ex, ex.messageType);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex, ex.messageType); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally
        {
            db.Close();
        }
        return ok;
    }


    public async Task<int> UpdatePurchase(Purchase purchase)
    {
        using var db = _DbContext.CreateConnection;
        int numberRows = 0;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                // Una orden con recepciones ya movió stock: editarla dejaría el
                // pedido y el inventario contando historias distintas.
                var currentStatus = await db.QuerySingleOrDefaultAsync<int?>(
                    "SELECT purchase_status_id FROM purchases WHERE id = @Id AND state FOR UPDATE",
                    new { purchase.Id }, transaction);

                if (currentStatus is null)
                    throw new CustomException("La orden de compra no existe.", MessageTypes.Warning);

                PurchaseReceiptPolicy.EnsureCanModify(currentStatus.Value);

                string sqlQuery = @"
                         UPDATE purchases
                           SET purchase_date= @PurchaseDate,
                               total= @Total,
                               is_active= @IsActive,
                               provider_id = @ProviderId,
                               estimated_delivery_date = @EstimatedDeliveryDate,
                               modified_by= @ModifiedBy,
                               modified= @Modified
                         WHERE id= @Id;
                    ";
                numberRows = await db.ExecuteAsync(sqlQuery, purchase, transaction);
                purchase.Detail.ForEach(x => { x.Modified = purchase.Modified; x.ModifiedBy = purchase.ModifiedBy; x.PurchaseStatusId = currentStatus.Value; });

                // Las líneas quitadas en la edición se dan de baja lógica.
                // Dapper expande la lista en el NOT IN, incluso vacía.
                var keepIds = purchase.Detail.Where(d => d.Id != Guid.Empty).Select(d => d.Id).ToList();
                await db.ExecuteAsync(@"
                        UPDATE purchases_detail
                           SET state = false, modified_by = @ModifiedBy, modified = @Modified
                         WHERE purchase_id = @PurchaseId
                           AND state
                           AND id NOT IN @KeepIds;
                    ", new { PurchaseId = purchase.Id, purchase.ModifiedBy, purchase.Modified, KeepIds = keepIds }, transaction);

                foreach (var detail in purchase.Detail)
                {
                    detail.State = true;
                    if (detail.Id == Guid.Empty)
                    {
                        detail.PurchaseId = purchase.Id;
                        detail.CreatedBy = purchase.ModifiedBy;
                        detail.Created = purchase.Modified;
                        await _purchaseDetailRepository.CreatePurchaseDetail(detail, db, transaction);
                    }
                    else
                    {
                        detail.PurchaseId = purchase.Id;
                        await _purchaseDetailRepository.UpdatePurchaseDetail(detail, db, transaction);
                    }
                }
                transaction.Commit();
            }
            catch (CustomException ex)
            {
                transaction.Rollback();
                throw new CustomException(ex.Message, ex, ex.messageType);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex, ex.messageType); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<int>(ex); }
        finally { db.Close(); }

        return numberRows;
    }

    /// <summary>
    /// Registra una recepción (total o parcial) contra una orden de compra.
    ///
    /// Todo ocurre en una sola transacción: se bloquea la orden, se releen los
    /// saldos, se validan las cantidades contra el pendiente real, se mueve el
    /// stock y se deriva el nuevo estado. Validar fuera de la transacción no
    /// sirve: entre la lectura y la escritura otro usuario puede recibir lo mismo.
    /// </summary>
    public async Task<int> ReceiveOrders(PurchaseDelivery purchaseDelivery)
    {
        int numberRows = 0;
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                // 1. Bloqueo pesimista: serializa recepciones sobre la misma orden.
                var currentStatus = await db.QuerySingleOrDefaultAsync<int?>(
                    "SELECT purchase_status_id FROM purchases WHERE id = @Id AND state FOR UPDATE",
                    new { Id = purchaseDelivery.PurchaseId }, transaction);

                if (currentStatus is null)
                    throw new CustomException("La orden de compra no existe.", MessageTypes.Warning);

                PurchaseReceiptPolicy.EnsureCanReceive(currentStatus.Value);

                // 2. Saldos reales y validación de las cantidades entrantes.
                var balances = await GetLineBalances(purchaseDelivery.PurchaseId, db, transaction);
                PurchaseReceiptPolicy.EnsureLinesAreReceivable(balances, purchaseDelivery.Detail);

                var pendingByProduct = balances.ToDictionary(b => b.ProductId, b => b.PendingQuantity);

                // 3. Cabecera de la recepción.
                purchaseDelivery.Id = Guid.NewGuid();
                string sqlQuery = @"INSERT INTO purchases_delivery
                                        (id, purchase_id, delivery_date, operation_uid, state, created_by, created, modified_by, modified)
                                  VALUES(@Id, @PurchaseId, @DeliveryDate, @OperationUid, @State, @CreatedBy, @Created, @ModifiedBy, @Modified); ";

                numberRows = await db.ExecuteAsync(sqlQuery, purchaseDelivery, transaction);

                // 4. Líneas efectivamente recibidas.
                foreach (var detail in purchaseDelivery.Detail.Where(d => d.DeliveryQuantity > 0))
                {
                    detail.PurchaseDeliveryId = purchaseDelivery.Id;
                    // Se registra el pendiente al momento de esta entrega, no el
                    // total original: así la fila dice "de 50 pendientes, recibí 30".
                    detail.OrderedQuantity = pendingByProduct[detail.ProductId];
                    await _purchaseDetailRepository.ReceiveOrdersDetail(purchaseDelivery.PurchaseId, detail, db, transaction);
                }

                // 5. Estado derivado de los saldos ya actualizados.
                var balancesAfter = await GetLineBalances(purchaseDelivery.PurchaseId, db, transaction);
                var newStatus = (int)PurchaseReceiptPolicy.DeriveStatus(balancesAfter);

                await db.ExecuteAsync(@"
                        UPDATE purchases
                           SET purchase_status_id = @PurchaseStatusId,
                               modified_by        = @ModifiedBy,
                               modified           = @Modified
                         WHERE id = @Id;
                    ", new
                {
                    Id = purchaseDelivery.PurchaseId,
                    PurchaseStatusId = newStatus,
                    purchaseDelivery.ModifiedBy,
                    purchaseDelivery.Modified
                }, transaction);

                await db.ExecuteAsync(@"
                        UPDATE purchases_detail
                           SET purchase_status_id = @PurchaseStatusId
                         WHERE purchase_id = @Id AND state;
                    ", new { Id = purchaseDelivery.PurchaseId, PurchaseStatusId = newStatus }, transaction);

                transaction.Commit();
                purchaseDelivery.PurchaseStatusId = newStatus;
            }
            catch (PostgresException ex) when (ex.ConstraintName == OperationUidConstraint)
            {
                // Reintento de una recepción ya aplicada: no se duplica el stock.
                transaction.Rollback();
                throw new CustomException("Esta recepción ya fue registrada.", MessageTypes.Info);
            }
            catch (CustomException ex)
            {
                transaction.Rollback();
                throw new CustomException(ex.Message, ex, ex.messageType);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex, ex.messageType); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<int>(ex); }
        finally { db.Close(); }
        return numberRows;
    }

    /// <summary>
    /// Cierra una orden parcialmente recibida: el proveedor no enviará el saldo.
    /// No mueve stock; solo impide nuevas recepciones y deja la orden fuera de
    /// los pendientes sin fingir que se recibió completa.
    /// </summary>
    public async Task<int> ClosePurchase(Guid id, int idUserModified)
    {
        using var db = _DbContext.CreateConnection;
        int numberRows;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                var currentStatus = await db.QuerySingleOrDefaultAsync<int?>(
                    "SELECT purchase_status_id FROM purchases WHERE id = @Id AND state FOR UPDATE",
                    new { Id = id }, transaction);

                if (currentStatus is null)
                    throw new CustomException("La orden de compra no existe.", MessageTypes.Warning);

                PurchaseReceiptPolicy.EnsureCanClose(currentStatus.Value);

                numberRows = await db.ExecuteAsync(@"
                        UPDATE purchases
                           SET purchase_status_id = @PurchaseStatusId,
                               modified_by        = @ModifiedBy,
                               modified           = @Modified
                         WHERE id = @Id;
                    ", new
                {
                    Id = id,
                    PurchaseStatusId = (int)Domain.Enums.PurchaseStatusEnum.CLOSED,
                    ModifiedBy = idUserModified,
                    Modified = DateTime.Now
                }, transaction);

                transaction.Commit();
            }
            catch (CustomException ex)
            {
                transaction.Rollback();
                throw new CustomException(ex.Message, ex, ex.messageType);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex, ex.messageType); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<int>(ex); }
        finally { db.Close(); }

        return numberRows;
    }

    /// <summary>Anula una orden que todavía no recibió nada.</summary>
    public async Task<int> CancelPurchase(Guid id, int idUserModified)
    {
        using var db = _DbContext.CreateConnection;
        int numberRows;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                var currentStatus = await db.QuerySingleOrDefaultAsync<int?>(
                    "SELECT purchase_status_id FROM purchases WHERE id = @Id AND state FOR UPDATE",
                    new { Id = id }, transaction);

                if (currentStatus is null)
                    throw new CustomException("La orden de compra no existe.", MessageTypes.Warning);

                var hasDeliveries = await db.ExecuteScalarAsync<bool>(
                    "SELECT EXISTS(SELECT 1 FROM purchases_delivery WHERE purchase_id = @Id AND state);",
                    new { Id = id }, transaction);

                PurchaseReceiptPolicy.EnsureCanCancel(currentStatus.Value, hasDeliveries);

                numberRows = await db.ExecuteAsync(@"
                        UPDATE purchases
                           SET purchase_status_id = @PurchaseStatusId,
                               modified_by        = @ModifiedBy,
                               modified           = @Modified
                         WHERE id = @Id;
                    ", new
                {
                    Id = id,
                    PurchaseStatusId = (int)Domain.Enums.PurchaseStatusEnum.CANCELLED,
                    ModifiedBy = idUserModified,
                    Modified = DateTime.Now
                }, transaction);

                transaction.Commit();
            }
            catch (CustomException ex)
            {
                transaction.Rollback();
                throw new CustomException(ex.Message, ex, ex.messageType);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex, ex.messageType); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<int>(ex); }
        finally { db.Close(); }

        return numberRows;
    }

    public async Task<List<PurchaseProductResponse>> GetPurchases(DateTime purchaseDateInitial, DateTime purchaseDateEnd, Domain.Enums.PurchaseStatusEnum purchaseStatus)
    {
        List<PurchaseProductResponse> listPurchases = [];
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            string sqlQuery = @"
                        SELECT p.id, p.purchase_date, p.total, p.is_active, ps.description as PurchaseStatusName, p.purchase_status_id,
                               p.provider_id, pr.provider_name, p.estimated_delivery_date
                          FROM purchases p
                               INNER JOIN purchases_status ps ON ps.id = p.purchase_status_id
                               INNER JOIN providers pr        ON pr.id = p.provider_id
                         WHERE p.state
                           AND p.is_active
                           AND p.purchase_date >= @PurchaseDateInitial
                           AND p.purchase_date <= @PurchaseDateEnd
                           AND p.purchase_status_id = @PurchaseStatusId
                           AND EXISTS (SELECT 1 FROM purchases_detail pd
                                        WHERE pd.purchase_id = p.id AND pd.state)
                         ORDER BY p.purchase_date DESC;
                ";

            var result = await db.QueryAsync<PurchaseProductResponse>(sqlQuery,
                new
                {
                    PurchaseDateInitial = purchaseDateInitial,
                    PurchaseDateEnd = purchaseDateEnd,
                    PurchaseStatusId = (int)purchaseStatus
                });
            listPurchases = result!.ToList();

            foreach (var item in listPurchases)
            {
                var resultDetail = await db.QueryAsync<PurchaseProductDetailResponse>(SqlPurchaseDetail, new { PurchaseId = item.Id });
                item.Detail = resultDetail.ToList();
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex, ex.messageType); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }

        return listPurchases;
    }

    public async Task<List<Purchase>> GetPurchases(string PurchaseDate)
    {
        List<Purchase> listPurchases = [];
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            string sqlQuery = @"
                        SELECT id, purchase_date, total, is_active
                         FROM purchases
                        WHERE state
                          AND purchase_date= @PurchaseDate;                ";

            var result = await db.QueryAsync<Purchase>(sqlQuery,
                new { PurchaseDate = PurchaseDate });
            listPurchases = result!.ToList();

        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex, ex.messageType); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }

        return listPurchases;
    }

    public async Task<PurchaseProductResponse> GetPurchase(Guid Id)
    {
        PurchaseProductResponse purchase = new();
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            string sqlQuery = @"
                        SELECT p.id, p.purchase_date, p.total, p.is_active, ps.description as PurchaseStatusName, p.purchase_status_id,
                            p.provider_id, pr.provider_name, p.estimated_delivery_date
                          FROM purchases p
                         INNER JOIN purchases_status ps ON ps.id = p.purchase_status_id
                         INNER JOIN providers pr        ON pr.id = p.provider_id
                         WHERE p.state
                           AND p.id = @Id;
                        ";

            purchase = await db.QueryFirstOrDefaultAsync<PurchaseProductResponse>(sqlQuery,
                new { Id }) ?? new PurchaseProductResponse();

            var resultDetail = await db.QueryAsync<PurchaseProductDetailResponse>(SqlPurchaseDetail, new { PurchaseId = purchase.Id });
            purchase.Detail = resultDetail.ToList();
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex, ex.messageType); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }

        return purchase;
    }

    public async Task<int> DeletePurchase(Guid id, int idUserModified)
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
                var currentStatus = await db.QuerySingleOrDefaultAsync<int?>(
                    "SELECT purchase_status_id FROM purchases WHERE id = @Id AND state FOR UPDATE",
                    new { Id = id }, transaction);

                if (currentStatus is null)
                    throw new CustomException("La orden de compra no existe.", MessageTypes.Warning);

                // Borrar una orden con recepciones dejaría movimientos de stock
                // apuntando a un documento inexistente.
                PurchaseReceiptPolicy.EnsureCanModify(currentStatus.Value);

                string sqlQuery = @"
                        UPDATE purchases
                           SET state = false,
                               modified_by = @ModifiedBy,
                               modified = @Modified
                         WHERE id = @Id ;
                    ";
                numberRows = await db.ExecuteAsync(sqlQuery, new { Id = id, ModifiedBy = idUserModified, @Modified = fechaActual }, transaction);
                transaction.Commit();
            }
            catch (CustomException ex)
            {
                transaction.Rollback();
                throw new CustomException(ex.Message, ex, ex.messageType);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex, ex.messageType); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }

        return numberRows;
    }

    private static async Task<List<PurchaseLineBalance>> GetLineBalances(Guid purchaseId, System.Data.IDbConnection db, System.Data.IDbTransaction transaction)
    {
        var balances = await db.QueryAsync<PurchaseLineBalance>(SqlLineBalances, new { PurchaseId = purchaseId }, transaction);
        return balances.ToList();
    }
}
