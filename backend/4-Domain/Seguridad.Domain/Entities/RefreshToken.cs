namespace Seguridad.Domain;

/// <summary>
/// Fila de sec.refresh_tokens. El valor en claro solo existe en el cliente:
/// aquí se guarda su hash.
/// </summary>
public class RefreshToken
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public int TenantId { get; set; }
    public string TokenHash { get; set; } = "";
    public string Device { get; set; } = "";
    public string LoginFrom { get; set; } = "";

    /// <summary>
    /// Id de sec.users_login (claim SessionId del JWT) vigente al emitir o
    /// rotar este refresh token. Sirve para revocar en memoria, de inmediato,
    /// el access token ya emitido cuando se cierra esta sesión puntual —
    /// revocar solo el refresh token no lo lograría hasta que ese access token
    /// expirara solo.
    /// </summary>
    public int SessionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public long? ReplacedBy { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsRevoked && !IsExpired;
}
