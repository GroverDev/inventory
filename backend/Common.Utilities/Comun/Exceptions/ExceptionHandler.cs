using Common.Utilities.Exceptions;
using Npgsql;

namespace Common.Utilities;

/// <summary>
/// Traduce una excepción de infraestructura al mensaje que verá el usuario.
/// </summary>
/// <remarks>
/// <para>
/// Antes, <b>todo</b> <see cref="PostgresException"/> se convertía en "No se
/// puede conectar a la Base de Datos". Es justo lo contrario de lo que significa:
/// esa excepción la produce el servidor <i>respondiendo</i>, así que la conexión
/// existió. Una función inexistente, una restricción violada o un permiso
/// denegado se veían idénticos a la base caída, y depurarlos exigía ir al log de
/// PostgreSQL. Pasó tres veces en una sola jornada.
/// </para>
/// <para>
/// El criterio ahora: los errores que la propia base escribió para una persona
/// (<c>RAISE EXCEPTION</c>) se muestran tal cual; las violaciones de restricción
/// se traducen a una frase entendible; el resto queda como error genérico
/// <b>pero se registra</b>, que es distinto de mentir sobre la causa.
/// </para>
/// </remarks>
public static class ExceptionHandler
{
    /// <summary>Errores que las funciones plpgsql lanzan con RAISE EXCEPTION.</summary>
    private const string RaiseException = "P0001";

    public static Exception HandleException<T>(Exception ex)
    {
        if (ex is PostgresException pg)
            return DePostgres(pg);

        if (ex is NpgsqlException npgsql)
            return DeNpgsql(npgsql);

        if (ex is InvalidOperationException invalid)
            return new Exception("Invalid Operation: " + invalid.Message, invalid);

        return new Exception("General Error:" + ex.Message, ex);
    }

    private static Exception DePostgres(PostgresException pg) => pg.SqlState switch
    {
        // Lo escribió una función nuestra pensando en quien lo va a leer:
        // "El número de serie X ya está registrado". No hay nada que traducir.
        RaiseException => new CustomException(pg.MessageText, MessageTypes.Warning),

        // Violaciones de restricción: el usuario necesita saber qué hizo mal,
        // no el nombre del índice ni la tabla. El detalle técnico va al log.
        PostgresErrorCodes.UniqueViolation => Registrado(pg,
            "Ya existe un registro con esos datos."),

        PostgresErrorCodes.ForeignKeyViolation => Registrado(pg,
            "El dato relacionado no existe, o no se puede eliminar porque está en uso."),

        PostgresErrorCodes.NotNullViolation => Registrado(pg,
            "Falta un dato obligatorio."),

        PostgresErrorCodes.CheckViolation => Registrado(pg,
            "Un dato no cumple una regla de validación."),

        // Todo lo demás (función inexistente, permiso denegado, error de
        // sintaxis) es un defecto del sistema, no algo que el usuario pueda
        // corregir. Se le da un mensaje genérico y se guarda el real.
        _ => Registrado(pg, "Ocurrió un error al procesar la operación."),
    };

    /// <summary>
    /// Mensaje para el usuario, causa real preservada y marcada para el log.
    /// </summary>
    private static CustomException Registrado(PostgresException pg, string mensaje) =>
        new($"{mensaje} ({pg.SqlState})", pg, saveLog: true);

    private static Exception DeNpgsql(NpgsqlException npgsql)
    {
        // Acá sí: no hubo servidor del otro lado.
        if (npgsql.InnerException is System.Net.Sockets.SocketException ||
            npgsql.Message.Contains("Failed to connect to", StringComparison.OrdinalIgnoreCase))
        {
            return new CustomException(
                "No se puede conectar a la base de datos. Verifique que el servidor esté " +
                "en funcionamiento y que haya conexión de red.",
                npgsql, saveLog: true);
        }

        var detalle = npgsql.InnerException is null
            ? npgsql.Message
            : $"{npgsql.Message} | {npgsql.InnerException.Message}";

        return new Exception("Npgsql Error: " + detalle, npgsql);
    }
}
