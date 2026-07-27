namespace Services.Api;

public static class RateLimitPolicies
{
    /// <summary>
    /// Límite por IP de origen para los endpoints anónimos de autenticación
    /// (login y verificación 2FA). Complementa al bloqueo por cuenta de
    /// <see cref="Seguridad.Domain.LoginSettings"/>: aquél frena el ataque
    /// dirigido a un usuario, éste frena el barrido de muchas cuentas.
    /// </summary>
    public const string Login = "login";
}
