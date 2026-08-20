namespace Seguridad.Domain;

/// <summary>Sesión activa (fila de sec.refresh_tokens) de un usuario, para pantallas de administración.</summary>
public class SessionResponse
{
    public long Id { get; set; }
    public string Device { get; set; } = "";
    public string LoginFrom { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

/// <summary>Sesión activa con datos del usuario, para el panel de "usuarios conectados" del tenant.</summary>
public class ConnectedUserResponse : SessionResponse
{
    public string Uuid { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
}
