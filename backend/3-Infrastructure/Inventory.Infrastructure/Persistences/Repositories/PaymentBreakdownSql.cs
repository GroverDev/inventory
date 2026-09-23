namespace Inventory.Infrastructure;

/// <summary>
/// Desglose por medio de pago, calculado en la base con la misma definición en
/// todos lados: cobrado = entregado − vuelto; menos lo reintegrado por
/// devoluciones con ese medio.
/// </summary>
/// <remarks>
/// Sumar en pantalla los pagos de cada venta da cifras falsas: el efectivo
/// contaría lo entregado y no lo cobrado, no restaría devoluciones, y en un
/// listado paginado sumaría solo la página visible.
/// </remarks>
internal static class PaymentBreakdownSql
{
    /// <summary>
    /// Arma la consulta sobre un CTE <c>ventas(id, grupo)</c> que el llamador
    /// define: qué ventas entran y a qué grupo pertenece cada una (el turno de
    /// caja, o una constante para un período). <paramref name="devolucionesSelect"/>
    /// aporta las devoluciones con las columnas (grupo, metodo, cobrado = 0,
    /// devuelto).
    /// </summary>
    public static string Build(string ventasCte, string devolucionesSelect) => $"""
        WITH ventas AS ({ventasCte}),
        movimientos AS (
            SELECT v.grupo, sp.payment_method_id AS metodo,
                   sum(sp.amount_given - COALESCE(sp."amount returned", 0)) AS cobrado,
                   0::numeric AS devuelto
              FROM sale_payments sp
              JOIN ventas v ON v.id = sp.sale_id
             GROUP BY v.grupo, sp.payment_method_id
            UNION ALL
            {devolucionesSelect}
        )
        SELECT m.grupo::text                             AS grupo,
               m.metodo                                  AS payment_method_id,
               COALESCE(pm.name, 'Sin medio registrado') AS name,
               COALESCE(pm.icon_css, '')                 AS icon_css,
               sum(m.cobrado)                            AS collected,
               sum(m.devuelto)                           AS refunded,
               sum(m.cobrado) - sum(m.devuelto)          AS net
          FROM movimientos m
          LEFT JOIN payment_methods pm ON pm.id = m.metodo
         GROUP BY m.grupo, m.metodo, pm.name, pm.icon_css
         ORDER BY m.grupo, sum(m.cobrado) - sum(m.devuelto) DESC
        """;
}
