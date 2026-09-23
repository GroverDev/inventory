using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;

namespace Inventory.Infrastructure;

public class CashSessionRepository(InventoryDbContext _DbContext, ICashMovementRepository _movementRepository) : ICashSessionRepository
{
    public async Task<Guid> OpenSession(CashSession session)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            session.Id = Guid.NewGuid();
            const string sql = @"
                INSERT INTO cash_sessions
                       (id, user_id, opened_at, opening_amount, state, created_by, created, modified_by, modified)
                VALUES (@Id, @UserId, @OpenedAt, @OpeningAmount, TRUE, @CreatedBy, @Created, @ModifiedBy, @Modified);";
            await db.ExecuteAsync(sql, session);
            return session.Id;
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }
    }

    public async Task<int> CloseSession(Guid sessionId, decimal declaredAmount, decimal expectedAmount, decimal difference, string notes, int modifiedBy)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            const string sql = @"
                UPDATE cash_sessions
                   SET closed_at       = NOW(),
                       declared_amount = @DeclaredAmount,
                       expected_amount = @ExpectedAmount,
                       difference      = @Difference,
                       notes           = @Notes,
                       modified_by     = @ModifiedBy,
                       modified        = NOW()
                 WHERE id = @SessionId AND closed_at IS NULL AND state = TRUE;";
            return await db.ExecuteAsync(sql, new { SessionId = sessionId, DeclaredAmount = declaredAmount, ExpectedAmount = expectedAmount, Difference = difference, Notes = notes, ModifiedBy = modifiedBy });
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }
    }

    public async Task<CashSessionResponse?> GetActiveSessionByUser(int userId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            const string sql = @"
                SELECT cs.id, cs.user_id, u.full_name AS UserFullName,
                       cs.opened_at, cs.closed_at,
                       cs.opening_amount, cs.declared_amount, cs.expected_amount, cs.difference, cs.notes, cs.close_attempts,
                       cs.close_authorized_by, COALESCE(ua.full_name, '') AS CloseAuthorizedByName,
                       COALESCE((SELECT SUM(s.total) FROM sales s WHERE s.cash_session_id = cs.id AND s.state), 0) AS TotalSales,
                       -- Solo lo que entra al cajón: si la venta se cobró por QR o tarjeta no hay
                       -- efectivo que contar. Se descuenta el vuelto entregado.
                       COALESCE(ROUND((SELECT SUM(sp.amount_given - COALESCE(sp.""amount returned"", 0))
                                   FROM sale_payments sp
                                        INNER JOIN payment_methods pm ON pm.id = sp.payment_method_id
                                        INNER JOIN sales sc ON sc.id = sp.sale_id
                                  WHERE sc.cash_session_id = cs.id AND sc.state AND pm.affects_cash), 2), 0) AS TotalCashSales,
                       COALESCE((SELECT SUM(m.amount) FROM cash_movements m WHERE m.cash_session_id = cs.id AND m.movement_type = 'expense' AND m.state), 0) AS TotalExpenses,
                       COALESCE((SELECT SUM(m.amount) FROM cash_movements m WHERE m.cash_session_id = cs.id AND m.movement_type = 'withdrawal' AND m.state), 0) AS TotalWithdrawals,
                       COALESCE((SELECT SUM(m.amount) FROM cash_movements m WHERE m.cash_session_id = cs.id AND m.movement_type = 'income' AND m.state), 0) AS TotalIncome,
                       COALESCE((SELECT SUM(m.amount) FROM cash_movements m WHERE m.cash_session_id = cs.id AND m.movement_type = 'return' AND m.state), 0) AS TotalReturns
                  FROM cash_sessions cs
                  JOIN sec.users u ON u.id = cs.user_id
                  LEFT JOIN sec.users ua ON ua.id = cs.close_authorized_by
                 WHERE cs.user_id = @UserId AND cs.closed_at IS NULL AND cs.state = TRUE
                   -- Una caja abierta por usuario y sucursal: la activa es la de
                   -- la sucursal donde está ahora.
                   AND cs.branch_id = public.current_branch();";
            var session = await db.QueryFirstOrDefaultAsync<CashSessionResponse>(sql, new { UserId = userId });
            if (session != null)
            {
                session.Movements = await _movementRepository.GetMovementsBySession(session.Id);
                await AttachPaymentBreakdown(db, [session]);
            }
            return session;
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }
    }

    public async Task<CashSessionResponse?> GetSessionById(Guid sessionId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            const string sql = @"
                SELECT cs.id, cs.user_id, u.full_name AS UserFullName,
                       cs.opened_at, cs.closed_at,
                       cs.opening_amount, cs.declared_amount, cs.expected_amount, cs.difference, cs.notes, cs.close_attempts,
                       cs.close_authorized_by, COALESCE(ua.full_name, '') AS CloseAuthorizedByName,
                       COALESCE((SELECT SUM(s.total) FROM sales s WHERE s.cash_session_id = cs.id AND s.state), 0) AS TotalSales,
                       -- Solo lo que entra al cajón: si la venta se cobró por QR o tarjeta no hay
                       -- efectivo que contar. Se descuenta el vuelto entregado.
                       COALESCE(ROUND((SELECT SUM(sp.amount_given - COALESCE(sp.""amount returned"", 0))
                                   FROM sale_payments sp
                                        INNER JOIN payment_methods pm ON pm.id = sp.payment_method_id
                                        INNER JOIN sales sc ON sc.id = sp.sale_id
                                  WHERE sc.cash_session_id = cs.id AND sc.state AND pm.affects_cash), 2), 0) AS TotalCashSales,
                       COALESCE((SELECT SUM(m.amount) FROM cash_movements m WHERE m.cash_session_id = cs.id AND m.movement_type = 'expense' AND m.state), 0) AS TotalExpenses,
                       COALESCE((SELECT SUM(m.amount) FROM cash_movements m WHERE m.cash_session_id = cs.id AND m.movement_type = 'withdrawal' AND m.state), 0) AS TotalWithdrawals,
                       COALESCE((SELECT SUM(m.amount) FROM cash_movements m WHERE m.cash_session_id = cs.id AND m.movement_type = 'income' AND m.state), 0) AS TotalIncome,
                       COALESCE((SELECT SUM(m.amount) FROM cash_movements m WHERE m.cash_session_id = cs.id AND m.movement_type = 'return' AND m.state), 0) AS TotalReturns
                  FROM cash_sessions cs
                  JOIN sec.users u ON u.id = cs.user_id
                  LEFT JOIN sec.users ua ON ua.id = cs.close_authorized_by
                 WHERE cs.id = @SessionId AND cs.state = TRUE;";
            var session = await db.QueryFirstOrDefaultAsync<CashSessionResponse>(sql, new { SessionId = sessionId });
            if (session != null)
            {
                session.Movements = await _movementRepository.GetMovementsBySession(session.Id);
                await AttachPaymentBreakdown(db, [session]);
            }
            return session;
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }
    }

    public async Task<List<SaleProductResponse>> GetSessionSales(Guid sessionId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();

            // v_sales_net y no sales, igual que el listado de ventas: si no, el
            // detalle de la sesión mostraría los importes facturados y una venta
            // devuelta seguiría contando entera.
            const string salesSql = @"
                SELECT s.id, s.customer_id, c.full_name AS CustomerName,
                       s.sale_date, s.subtotal, s.total_discounts,
                       COALESCE(s.header_discount_amount, 0) AS HeaderDiscountAmount,
                       s.total, s.is_active,
                       s.total_returned, s.net_total, s.sale_status,
                       COALESCE(u.full_name, '') AS SellerName
                  FROM v_sales_net s
                 INNER JOIN customers c ON c.id = s.customer_id
                 LEFT  JOIN sec.users u ON u.id = s.created_by
                 WHERE s.state
                   AND s.cash_session_id = @SessionId
                 ORDER BY s.sale_date ASC;";

            var sales = (await db.QueryAsync<SaleProductResponse>(salesSql, new { SessionId = sessionId })).ToList();

            const string detailSql = @"
                SELECT sd.id, sd.sale_id, sd.product_id,
                       p.product_name, sd.quantity, sd.unit_price,
                       sd.line_subtotal, sd.line_total_discounts, sd.line_total
                  FROM sales_detail sd
                 INNER JOIN products p ON p.id = sd.product_id
                 WHERE sd.sale_id = @SaleId;";

            const string paymentSql = @"
                SELECT sp.id, sp.payment_method_id,
                       pm.name AS PaymentMethodName, pm.icon_css AS IconCss,
                       sp.amount_given AS AmountGiven, sp.""amount returned"" AS AmountReturned
                  FROM sale_payments sp
                  JOIN payment_methods pm ON pm.id = sp.payment_method_id
                 WHERE sp.sale_id = @SaleId;";

            foreach (var sale in sales)
            {
                sale.Detail = (await db.QueryAsync<SaleProductDetailResponse>(detailSql, new { SaleId = sale.Id })).ToList();
                sale.Payments = (await db.QueryAsync<SalePaymentResponse>(paymentSql, new { SaleId = sale.Id })).ToList();
            }

            return sales;
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }
    }

    public async Task<List<CashSessionResponse>> GetSessions(DateTime dateFrom, DateTime dateTo, int? userId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            string userFilter = userId.HasValue ? "AND cs.user_id = @UserId" : "";
            string sql = $@"
                SELECT cs.id, cs.user_id, u.full_name AS UserFullName,
                       cs.opened_at, cs.closed_at,
                       cs.opening_amount, cs.declared_amount, cs.expected_amount, cs.difference, cs.notes, cs.close_attempts,
                       cs.close_authorized_by, COALESCE(ua.full_name, '') AS CloseAuthorizedByName,
                       COALESCE((SELECT SUM(s.total) FROM sales s WHERE s.cash_session_id = cs.id AND s.state), 0) AS TotalSales,
                       -- Solo lo que entra al cajón: si la venta se cobró por QR o tarjeta no hay
                       -- efectivo que contar. Se descuenta el vuelto entregado.
                       COALESCE(ROUND((SELECT SUM(sp.amount_given - COALESCE(sp.""amount returned"", 0))
                                   FROM sale_payments sp
                                        INNER JOIN payment_methods pm ON pm.id = sp.payment_method_id
                                        INNER JOIN sales sc ON sc.id = sp.sale_id
                                  WHERE sc.cash_session_id = cs.id AND sc.state AND pm.affects_cash), 2), 0) AS TotalCashSales,
                       COALESCE((SELECT SUM(m.amount) FROM cash_movements m WHERE m.cash_session_id = cs.id AND m.movement_type = 'expense' AND m.state), 0) AS TotalExpenses,
                       COALESCE((SELECT SUM(m.amount) FROM cash_movements m WHERE m.cash_session_id = cs.id AND m.movement_type = 'withdrawal' AND m.state), 0) AS TotalWithdrawals,
                       COALESCE((SELECT SUM(m.amount) FROM cash_movements m WHERE m.cash_session_id = cs.id AND m.movement_type = 'income' AND m.state), 0) AS TotalIncome,
                       COALESCE((SELECT SUM(m.amount) FROM cash_movements m WHERE m.cash_session_id = cs.id AND m.movement_type = 'return' AND m.state), 0) AS TotalReturns
                  FROM cash_sessions cs
                  JOIN sec.users u ON u.id = cs.user_id
                  LEFT JOIN sec.users ua ON ua.id = cs.close_authorized_by
                 WHERE cs.state = TRUE
                   AND cs.branch_id = public.current_branch()
                   AND (cs.closed_at IS NULL OR (cs.opened_at >= @DateFrom AND cs.opened_at < @DateTo))
                   {userFilter}
                 ORDER BY cs.opened_at DESC;";
            var sessions = (await db.QueryAsync<CashSessionResponse>(sql, new { DateFrom = dateFrom, DateTo = dateTo, UserId = userId })).ToList();
            await AttachPaymentBreakdown(db, sessions);
            return sessions;
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }
    }

    /// <summary>
    /// Completa el desglose por medio de pago de cada turno, en una sola consulta
    /// para todos.
    /// </summary>
    private static async Task AttachPaymentBreakdown(System.Data.IDbConnection db, List<CashSessionResponse> sessions)
    {
        if (sessions.Count == 0) return;

        const string ventasDeLosTurnos = @"
            SELECT s.id, s.cash_session_id AS grupo
              FROM sales s
             WHERE s.state AND s.cash_session_id = ANY(@Ids)";
        // Las devoluciones registradas en el turno (las en efectivo; un reintegro
        // por QR o tarjeta no queda asociado a ningún turno).
        const string devoluciones = @"
            SELECT sr.cash_session_id, sr.payment_method_id, 0::numeric, sum(sr.total_returned)
              FROM sale_returns sr
             WHERE sr.state AND sr.cash_session_id = ANY(@Ids)
             GROUP BY sr.cash_session_id, sr.payment_method_id";

        var filas = await db.QueryAsync<BreakdownRow>(
            PaymentBreakdownSql.Build(ventasDeLosTurnos, devoluciones),
            new { Ids = sessions.Select(x => x.Id).ToArray() });

        var porTurno = filas.GroupBy(f => f.Grupo).ToDictionary(g => g.Key, g => g.Cast<PaymentMethodTotal>().ToList());
        foreach (var session in sessions)
            session.ByPaymentMethod = porTurno.GetValueOrDefault(session.Id.ToString(), []);

        // Arqueo del cierre por medio, de las que ya cerraron.
        var arqueos = (await db.QueryAsync<CountRow>(@"
            SELECT c.cash_session_id, c.payment_method_id, COALESCE(pm.name, '') AS name,
                   COALESCE(pm.icon_css, '') AS icon_css, c.expected, c.declared, c.difference
              FROM cash_session_counts c
              LEFT JOIN payment_methods pm ON pm.id = c.payment_method_id
             WHERE c.cash_session_id = ANY(@Ids)
             ORDER BY pm.name",
            new { Ids = sessions.Select(x => x.Id).ToArray() }))
            .GroupBy(a => a.CashSessionId)
            .ToDictionary(g => g.Key, g => g.Cast<CashCountResponse>().ToList());
        foreach (var session in sessions)
            session.Counts = arqueos.GetValueOrDefault(session.Id, []);

        // Conteo del efectivo por billete y moneda.
        var billetes = (await db.QueryAsync<DenominationRow>(@"
            SELECT cash_session_id, value, quantity
              FROM cash_session_denominations
             WHERE cash_session_id = ANY(@Ids)
             ORDER BY value DESC",
            new { Ids = sessions.Select(x => x.Id).ToArray() }))
            .GroupBy(d => d.CashSessionId)
            .ToDictionary(g => g.Key, g => g.Cast<DenominationCount>().ToList());
        foreach (var session in sessions)
            session.Denominations = billetes.GetValueOrDefault(session.Id, []);
    }

    private class DenominationRow : DenominationCount
    {
        public Guid CashSessionId { get; set; }
    }

    private class CountRow : CashCountResponse
    {
        public Guid CashSessionId { get; set; }
    }

    public async Task<int> CloseSessionWithCounts(Guid sessionId, decimal declaredAmount, decimal expectedAmount, decimal difference,
                                                  string notes, int modifiedBy, List<CashCountResponse> counts,
                                                  int? authorizedBy, List<DenominationCount> denominations)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                int filas = await db.ExecuteAsync(@"
                    UPDATE cash_sessions
                       SET closed_at       = NOW(),
                           declared_amount = @DeclaredAmount,
                           expected_amount = @ExpectedAmount,
                           difference      = @Difference,
                           notes           = @Notes,
                           close_authorized_by = @AuthorizedBy,
                           modified_by     = @ModifiedBy,
                           modified        = NOW()
                     WHERE id = @SessionId AND closed_at IS NULL AND state = TRUE",
                    new { SessionId = sessionId, DeclaredAmount = declaredAmount, ExpectedAmount = expectedAmount,
                          Difference = difference, Notes = notes, ModifiedBy = modifiedBy, AuthorizedBy = authorizedBy }, transaction);

                if (filas > 0)
                {
                    await db.ExecuteAsync(@"
                        INSERT INTO cash_session_counts (cash_session_id, payment_method_id, expected, declared, difference)
                        VALUES (@SessionId, @PaymentMethodId, @Expected, @Declared, @Difference)",
                        counts.Select(c => new { SessionId = sessionId, c.PaymentMethodId, c.Expected, c.Declared, c.Difference }),
                        transaction);
                    await db.ExecuteAsync(@"
                        INSERT INTO cash_session_denominations (cash_session_id, value, quantity)
                        VALUES (@SessionId, @Value, @Quantity)",
                        denominations.Select(d => new { SessionId = sessionId, d.Value, d.Quantity }),
                        transaction);
                }

                transaction.Commit();
                return filas;
            }
            catch { transaction.Rollback(); throw; }
        }
        catch (Exception ex) { throw Common.Utilities.ExceptionHandler.HandleException<int>(ex); }
        finally { db.Close(); }
    }

    public async Task IncrementCloseAttempts(Guid sessionId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            await db.ExecuteAsync(
                "UPDATE cash_sessions SET close_attempts = close_attempts + 1 WHERE id = @sessionId AND closed_at IS NULL",
                new { sessionId });
        }
        catch (Exception ex) { throw Common.Utilities.ExceptionHandler.HandleException<bool>(ex); }
        finally { db.Close(); }
    }

    public async Task<CashCloseSettings> GetCloseSettings()
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            return await db.QueryFirstOrDefaultAsync<CashCloseSettings>(@"
                SELECT note_threshold, supervisor_threshold, max_attempts, require_denominations
                  FROM cash_close_settings") ?? new CashCloseSettings();
        }
        catch (Exception ex) { throw Common.Utilities.ExceptionHandler.HandleException<CashCloseSettings>(ex); }
        finally { db.Close(); }
    }

    public async Task SaveCloseSettings(CashCloseSettingsRequest settings, int userId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            await db.ExecuteAsync(@"
                INSERT INTO cash_close_settings (note_threshold, supervisor_threshold, max_attempts, require_denominations, modified_by)
                VALUES (@NoteThreshold, @SupervisorThreshold, @MaxAttempts, @RequireDenominations, @userId)
                ON CONFLICT (tenant_id) DO UPDATE
                   SET note_threshold        = EXCLUDED.note_threshold,
                       supervisor_threshold  = EXCLUDED.supervisor_threshold,
                       max_attempts          = EXCLUDED.max_attempts,
                       require_denominations = EXCLUDED.require_denominations,
                       modified_by = EXCLUDED.modified_by, modified = now()",
                new { settings.NoteThreshold, settings.SupervisorThreshold, settings.MaxAttempts,
                      settings.RequireDenominations, userId });
        }
        catch (Exception ex) { throw Common.Utilities.ExceptionHandler.HandleException<bool>(ex); }
        finally { db.Close(); }
    }

    private class BreakdownRow : PaymentMethodTotal
    {
        public string Grupo { get; set; } = "";
    }
}
