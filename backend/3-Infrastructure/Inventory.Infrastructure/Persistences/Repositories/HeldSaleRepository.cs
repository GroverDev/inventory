using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;

namespace Inventory.Infrastructure;

/// <remarks>
/// Todo acotado a la sucursal activa: una venta en espera es de la sucursal, y
/// cualquier caja de ella la puede retomar. Las de otra sucursal no se ven ni se
/// tocan desde acá.
/// </remarks>
public class HeldSaleRepository(InventoryDbContext _DbContext) : IHeldSaleRepository
{
    /// <summary>Tope por sucursal: más que esto ya no es una espera sino un olvido.</summary>
    private const int MaxPorSucursal = 50;

    public async Task<Guid> Hold(HeldSaleRequest request, int userId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            int enEspera = await db.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM held_sales WHERE branch_id = public.current_branch()");
            if (enEspera >= MaxPorSucursal)
                throw new CustomException(
                    $"Ya hay {enEspera} ventas en espera en esta sucursal. Cobre o descarte alguna antes de dejar otra.");

            return await db.ExecuteScalarAsync<Guid>(@"
                INSERT INTO held_sales (user_id, customer_id, label, items_count, total, payload)
                VALUES (@UserId, @CustomerId, @Label, @ItemsCount, @Total, @Payload::jsonb)
                RETURNING id",
                new { UserId = userId, request.CustomerId, Label = request.Label.Trim(), request.ItemsCount, request.Total, request.Payload });
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<Guid>(ex); }
        finally { db.Close(); }
    }

    public async Task<List<HeldSaleResponse>> GetHeld()
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            var rows = await db.QueryAsync<HeldSaleResponse>(@"
                SELECT h.id, h.label, COALESCE(c.full_name, '') AS customer_name,
                       COALESCE(u.full_name, '') AS user_name,
                       h.created, h.items_count, h.total
                  FROM held_sales h
                  LEFT JOIN customers c ON c.id = h.customer_id
                  LEFT JOIN sec.users u ON u.id = h.user_id
                 WHERE h.branch_id = public.current_branch()
                 ORDER BY h.created");
            return rows.ToList();
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<List<HeldSaleResponse>>(ex); }
        finally { db.Close(); }
    }

    public async Task<HeldSaleResponse?> Take(Guid id)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            // DELETE ... RETURNING: sacarla y leerla es un solo paso atómico, así
            // que dos cajas que la retomen a la vez no pueden cobrarla dos veces.
            return await db.QueryFirstOrDefaultAsync<HeldSaleResponse>(@"
                DELETE FROM held_sales
                 WHERE id = @id AND branch_id = public.current_branch()
                RETURNING id, label, created, items_count, total, payload::text AS payload",
                new { id });
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<HeldSaleResponse>(ex); }
        finally { db.Close(); }
    }

    public async Task<bool> Discard(Guid id)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            return await db.ExecuteAsync(
                "DELETE FROM held_sales WHERE id = @id AND branch_id = public.current_branch()",
                new { id }) > 0;
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally { db.Close(); }
    }
}
