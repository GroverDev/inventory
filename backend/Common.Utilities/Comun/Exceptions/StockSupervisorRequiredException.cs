namespace Common.Utilities.Exceptions;

/// <summary>
/// Una venta pide más de lo que hay de un producto sin seguimiento, y quien la
/// hace es un cajero sin autorización de supervisor.
/// </summary>
/// <remarks>
/// No es un error de datos: es "falta una firma". El punto de venta lo reconoce
/// (ver <see cref="MessageId"/>) y pide las credenciales de un supervisor en vez
/// de mostrar un rechazo. La regla vive en la base (código PS001 de
/// <c>fn_asignar_fefo</c>): ahí la comprobación y el descuento son atómicos.
/// </remarks>
public class StockSupervisorRequiredException(string message) : CustomException(message, MessageTypes.Warning)
{
    /// <summary>Valor de <c>Message.Id</c> con el que la API le avisa al cliente.</summary>
    public const string MessageId = "requires-supervisor-stock";

    /// <summary>Código SQLSTATE con el que la base lanza este caso.</summary>
    public const string SqlState = "PS001";

    /// <summary>¿Esta excepción, o alguna de su cadena, es de este tipo?</summary>
    public static bool EnCadena(Exception? ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
            if (e is StockSupervisorRequiredException) return true;
        return false;
    }
}
