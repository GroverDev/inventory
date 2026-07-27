namespace Seguridad.Domain;

/// <summary>
/// Freno de fuerza bruta para el login con contraseña. Mismo criterio que
/// <see cref="MfaSettings"/>, pero aplicado sobre los intentos registrados en
/// sec.users_login.
/// </summary>
public class LoginSettings
{
    public int MaxFailedAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
}
