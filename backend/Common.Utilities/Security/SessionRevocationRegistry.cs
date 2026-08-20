using System.Collections.Concurrent;

namespace Common.Utilities.Security;

/// <summary>
/// Revocación de sesiones en memoria: permite cerrar una sesión ya en curso al
/// instante, sin esperar a que el access token ya emitido venza solo.
/// </summary>
/// <remarks>
/// Vive en memoria de un solo proceso a propósito, mismo criterio que
/// TurnstileCircuitBreaker: revocar en base de datos (sec.refresh_tokens)
/// impide renovar la sesión, pero no tumba el access token que el cliente ya
/// tiene en la mano —es un JWT autocontenido, nadie lo consulta contra la
/// base en cada request—. Este registro es lo que sí lo tumba, comparando el
/// claim SessionId de cada request contra la lista.
/// <para>
/// Un reinicio del backend "olvida" las revocaciones hechas antes de ese
/// momento: el riesgo queda acotado a lo que le faltara de vida a esos
/// tokens (JwtSettings.TimeTokenRefreshable), no es indefinido. Si se llega a
/// correr más de una instancia del backend a la vez, esto deja de alcanzar y
/// habría que moverlo a un almacén compartido (Redis).
/// </para>
/// </remarks>
public class SessionRevocationRegistry
{
    private readonly ConcurrentDictionary<int, byte> _revokedSessionIds = new();

    public void Revoke(int sessionId) => _revokedSessionIds[sessionId] = 0;

    public void RevokeMany(IEnumerable<int> sessionIds)
    {
        foreach (var id in sessionIds) Revoke(id);
    }

    public bool IsRevoked(int sessionId) => _revokedSessionIds.ContainsKey(sessionId);
}
