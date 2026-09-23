using System.Data;
using System.Text.Json;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;

namespace Inventory.Infrastructure;

/// <remarks>
/// Este repositorio no mueve stock: enviar y recibir llaman a
/// <c>fn_enviar_traspaso</c> y <c>fn_recibir_traspaso</c>, que lo hacen en una sola
/// transacción. Acá solo se arman y leen los borradores.
/// </remarks>
public class StockTransferRepository(InventoryDbContext _DbContext) : IStockTransferRepository
{
    private const string SelectCabecera = """
        SELECT t.id, t.number, t.origin_branch_id, bo.name AS OriginBranchName,
               t.dest_branch_id, bd.name AS DestBranchName,
               t.status, t.notes,
               CASE WHEN t.origin_branch_id = public.current_branch() THEN 'salida' ELSE 'entrada' END AS Direction,
               t.created, COALESCE(uc.full_name, '') AS CreatedByName,
               t.sent_at, COALESCE(us.full_name, '') AS SentByName,
               t.received_at, COALESCE(ur.full_name, '') AS ReceivedByName,
               (SELECT count(*)::int FROM stock_transfer_detail d WHERE d.transfer_id = t.id) AS LinesCount,
               (SELECT COALESCE(sum(d.quantity), 0) FROM stock_transfer_detail d WHERE d.transfer_id = t.id) AS TotalQuantity
          FROM stock_transfers t
          JOIN branches bo ON bo.id = t.origin_branch_id
          JOIN branches bd ON bd.id = t.dest_branch_id
          LEFT JOIN sec.users uc ON uc.id = t.created_by
          LEFT JOIN sec.users us ON us.id = t.sent_by
          LEFT JOIN sec.users ur ON ur.id = t.received_by
         WHERE t.state
           AND public.current_branch() IN (t.origin_branch_id, t.dest_branch_id)
        """;

    public async Task<Guid> CreateDraft(StockTransferRequest request, int userId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                await ValidarDestino(db, transaction, request.DestBranchId);

                // Correlativo por farmacia. El candado de transacción serializa
                // solo las altas de traspasos de esta farmacia; se libera solo al
                // terminar.
                await db.ExecuteAsync(
                    "SELECT pg_advisory_xact_lock(hashtext('stock_transfers'), public.current_tenant())",
                    transaction: transaction);

                var id = await db.ExecuteScalarAsync<Guid>(@"
                    INSERT INTO stock_transfers
                           (number, origin_branch_id, dest_branch_id, notes, created_by, modified_by)
                    VALUES ((SELECT COALESCE(max(number), 0) + 1 FROM stock_transfers),
                            public.current_branch(), @Dest, @Notes, @UserId, @UserId)
                    RETURNING id",
                    new { Dest = request.DestBranchId, request.Notes, UserId = userId }, transaction);

                await InsertarDetalle(db, transaction, id, request.Detail);

                transaction.Commit();
                return id;
            }
            catch { transaction.Rollback(); throw; }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<Guid>(ex); }
        finally { db.Close(); }
    }

    public async Task UpdateDraft(Guid id, StockTransferRequest request, int userId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                await BloquearBorradorPropio(db, transaction, id);
                await ValidarDestino(db, transaction, request.DestBranchId);

                await db.ExecuteAsync(@"
                    UPDATE stock_transfers
                       SET dest_branch_id = @Dest, notes = @Notes, modified_by = @UserId, modified = now()
                     WHERE id = @Id",
                    new { Id = id, Dest = request.DestBranchId, request.Notes, UserId = userId }, transaction);

                await db.ExecuteAsync("DELETE FROM stock_transfer_detail WHERE transfer_id = @Id", new { Id = id }, transaction);
                await InsertarDetalle(db, transaction, id, request.Detail);

                transaction.Commit();
            }
            catch { transaction.Rollback(); throw; }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally { db.Close(); }
    }

    public async Task Cancel(Guid id, int userId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                // Solo un borrador: uno enviado ya movió stock, y lo que está en
                // camino se resuelve recibiéndolo (con faltante, si no llegó).
                await BloquearBorradorPropio(db, transaction, id);

                await db.ExecuteAsync(@"
                    UPDATE stock_transfers
                       SET status = 'anulado', modified_by = @UserId, modified = now()
                     WHERE id = @Id",
                    new { Id = id, UserId = userId }, transaction);

                transaction.Commit();
            }
            catch { transaction.Rollback(); throw; }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally { db.Close(); }
    }

    public async Task Send(Guid id, int userId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            // La función valida estado, sucursal y stock, y mueve todo o nada.
            await db.ExecuteAsync("SELECT fn_enviar_traspaso(@Id, @UserId)", new { Id = id, UserId = userId });
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally { db.Close(); }
    }

    public async Task Receive(Guid id, List<ReceivedLotRequest> lots, int userId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            // {"<id del lote>": cantidad}. Los lotes que no vienen se reciben completos.
            string recibido = JsonSerializer.Serialize(
                lots.ToDictionary(l => l.LotId.ToString(), l => l.QuantityReceived));

            await db.ExecuteAsync("SELECT fn_recibir_traspaso(@Id, @UserId, @Recibido::jsonb)",
                new { Id = id, UserId = userId, Recibido = recibido });
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally { db.Close(); }
    }

    public async Task<List<StockTransferResponse>> GetTransfers(DateTime from, DateTime to, string status)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            var result = await db.QueryAsync<StockTransferResponse>(SelectCabecera + """

                   AND (@Status = '' OR t.status = @Status)
                   -- Lo pendiente se ve siempre, sin importar la fecha: un traspaso
                   -- en camino desde hace una semana es justamente el que hay que ver.
                   AND (t.status IN ('borrador', 'enviado') OR (t.created >= @From AND t.created < @To))
                 ORDER BY t.number DESC
                """,
                new { From = from, To = to, Status = status ?? "" });
            return result.ToList();
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<List<StockTransferResponse>>(ex); }
        finally { db.Close(); }
    }

    public async Task<StockTransferResponse?> GetTransfer(Guid id)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            var transfer = await db.QueryFirstOrDefaultAsync<StockTransferResponse>(
                SelectCabecera + "\n   AND t.id = @Id", new { Id = id });
            if (transfer == null) return null;

            transfer.Detail = (await db.QueryAsync<StockTransferDetailResponse>(@"
                SELECT d.id, d.transfer_id, d.product_id, COALESCE(p.product_code, '') AS product_code,
                       p.product_name, p.tracking_mode, d.quantity,
                       COALESCE(vs.quantity, 0) AS available_at_origin
                  FROM stock_transfer_detail d
                  JOIN products p ON p.id = d.product_id
                  LEFT JOIN v_stock_sucursal vs ON vs.product_id = d.product_id AND vs.branch_id = @Origin
                 WHERE d.transfer_id = @Id
                 ORDER BY p.product_name",
                new { Id = id, Origin = transfer.OriginBranchId })).ToList();

            var lotes = (await db.QueryAsync<StockTransferLotResponse>(@"
                SELECT l.id, l.detail_id, COALESCE(l.lot_code, '') AS lot_code, l.expiry_date,
                       COALESCE(l.serial_number, '') AS serial_number, l.quantity_sent, l.quantity_received
                  FROM stock_transfer_lots l
                  JOIN stock_transfer_detail d ON d.id = l.detail_id
                 WHERE d.transfer_id = @Id
                 ORDER BY l.expiry_date NULLS FIRST, l.lot_code, l.serial_number",
                new { Id = id })).ToList();

            foreach (var d in transfer.Detail)
                d.Lots = lotes.Where(l => l.DetailId == d.Id).ToList();

            return transfer;
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<StockTransferResponse>(ex); }
        finally { db.Close(); }
    }

    private static async Task ValidarDestino(IDbConnection db, IDbTransaction transaction, Guid destino)
    {
        var fila = await db.QueryFirstOrDefaultAsync<(bool Activa, bool EsLaActual)>(@"
            SELECT is_active AND state, id = public.current_branch()
              FROM branches WHERE id = @destino",
            new { destino }, transaction);

        if (fila == default || !fila.Activa)
            throw new CustomException("La sucursal de destino no existe o está inactiva.");
        if (fila.EsLaActual)
            throw new CustomException("El destino tiene que ser otra sucursal: el traspaso sale de la sucursal en la que está.");
    }

    /// <summary>Bloquea el traspaso y exige que sea un borrador de la sucursal activa.</summary>
    private static async Task BloquearBorradorPropio(IDbConnection db, IDbTransaction transaction, Guid id)
    {
        var fila = await db.QueryFirstOrDefaultAsync<(string Status, bool EsOrigen, int Number)>(@"
            SELECT status, origin_branch_id = public.current_branch(), number
              FROM stock_transfers WHERE id = @id AND state FOR UPDATE",
            new { id }, transaction);

        if (fila == default)
            throw new CustomException("No existe el traspaso.");
        if (!fila.EsOrigen)
            throw new CustomException($"El traspaso {fila.Number} se modifica desde su sucursal de origen.");
        if (fila.Status != "borrador")
            throw new CustomException($"El traspaso {fila.Number} ya está {fila.Status}: solo se modifica un borrador.");
    }

    private static async Task InsertarDetalle(IDbConnection db, IDbTransaction transaction, Guid id, List<StockTransferLineRequest> detalle)
    {
        await db.ExecuteAsync(@"
            INSERT INTO stock_transfer_detail (transfer_id, product_id, quantity)
            VALUES (@TransferId, @ProductId, @Quantity)",
            detalle.Select(l => new { TransferId = id, l.ProductId, l.Quantity }), transaction);
    }
}
