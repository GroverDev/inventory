namespace Common.Utilities.MultiTenancy;

/// <summary>
/// Tenant al que pertenece el request en curso.
/// </summary>
/// <remarks>
/// Se registra como <c>Scoped</c>: una instancia por request. El middleware lo
/// puebla a partir del claim del JWT y los contextos de datos lo consumen para
/// fijar <c>app.tenant_id</c> en cada conexión, que es lo que usan las políticas
/// de Row-Level Security para filtrar.
/// </remarks>
public interface ITenantContext
{
    /// <summary>Tenant del request, o <c>null</c> si todavía no se resolvió.</summary>
    int? TenantId { get; }

    /// <summary>
    /// <c>false</c> en los endpoints anónimos (login, health check), donde
    /// todavía no se sabe quién entra.
    /// </summary>
    bool HasTenant { get; }

    /// <summary>Fija el tenant del request. Solo puede llamarse una vez.</summary>
    void SetTenant(int tenantId);
}

/// <inheritdoc cref="ITenantContext"/>
public sealed class TenantContext : ITenantContext
{
    public int? TenantId { get; private set; }

    public bool HasTenant => TenantId.HasValue;

    public void SetTenant(int tenantId)
    {
        if (tenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tenantId), "El tenant debe ser un entero positivo.");

        // Un request pertenece a un solo tenant de principio a fin. Si algo
        // intenta reasignarlo a mitad de camino es un bug, y conviene que
        // reviente acá y no que termine escribiendo en la base equivocada.
        if (TenantId.HasValue && TenantId.Value != tenantId)
            throw new InvalidOperationException(
                $"El tenant del request ya está fijado en {TenantId.Value} y no puede cambiarse a {tenantId}.");

        TenantId = tenantId;
    }
}
