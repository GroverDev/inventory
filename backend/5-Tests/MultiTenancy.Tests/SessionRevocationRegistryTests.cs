using Common.Utilities.Security;

namespace MultiTenancy.Tests;

/// <summary>
/// El registro en memoria que hace instantáneo el cierre de una sesión: sin
/// él, revocar el refresh token solo impide renovar, pero el access token ya
/// emitido —un JWT autocontenido— seguiría sirviendo hasta vencer solo.
/// No necesita base de datos: es lógica pura en memoria.
/// </summary>
public class SessionRevocationRegistryTests
{
    [Fact]
    public void Una_sesion_no_revocada_no_esta_revocada()
    {
        var registry = new SessionRevocationRegistry();
        Assert.False(registry.IsRevoked(1));
    }

    [Fact]
    public void Revoke_marca_esa_sesion_como_revocada()
    {
        var registry = new SessionRevocationRegistry();
        registry.Revoke(42);

        Assert.True(registry.IsRevoked(42));
    }

    [Fact]
    public void Revoke_no_afecta_a_otras_sesiones()
    {
        var registry = new SessionRevocationRegistry();
        registry.Revoke(42);

        Assert.False(registry.IsRevoked(43));
    }

    [Fact]
    public void RevokeMany_revoca_todas_las_indicadas()
    {
        var registry = new SessionRevocationRegistry();
        registry.RevokeMany([10, 20, 30]);

        Assert.True(registry.IsRevoked(10));
        Assert.True(registry.IsRevoked(20));
        Assert.True(registry.IsRevoked(30));
        Assert.False(registry.IsRevoked(40));
    }
}
