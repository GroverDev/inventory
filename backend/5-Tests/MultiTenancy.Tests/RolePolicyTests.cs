using Common.Utilities.Comun.Bases;

namespace MultiTenancy.Tests;

/// <summary>
/// La regla que decide si un usuario ve solo sus propios datos.
/// </summary>
/// <remarks>
/// No necesita base de datos. Cubre la regla «gana el más privilegiado»: un
/// usuario queda restringido solo si <c>Cajero</c> es su único rol.
/// </remarks>
public class RolePolicyTests
{
    [Theory]
    [InlineData("Cajero")]
    [InlineData(" Cajero ")]
    [InlineData("cajero")]          // la comparación ignora mayúsculas
    public void Cajero_solo_ve_lo_propio(string roles)
        => Assert.True(RolePolicy.VeSoloLoPropio(roles));

    [Theory]
    [InlineData("Administrador,Cajero")]
    [InlineData("Cajero,Administrador")]
    [InlineData("Cajero,SuperAdmin")]
    public void Cajero_con_otro_rol_ve_todo(string roles)
        => Assert.False(RolePolicy.VeSoloLoPropio(roles));

    [Theory]
    [InlineData("Administrador")]
    [InlineData("SuperAdmin")]
    [InlineData("Administrador,SuperAdmin")]
    public void Quien_no_es_cajero_ve_todo(string roles)
        => Assert.False(RolePolicy.VeSoloLoPropio(roles));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Sin_roles_no_se_restringe(string? roles)
    {
        // Un usuario sin roles no es cajero. La restricción es la excepción, no el
        // caso base; lo que limite su acceso serán los permisos por formulario.
        Assert.False(RolePolicy.VeSoloLoPropio(roles));
    }

    [Fact]
    public void Tolera_separadores_sueltos()
        => Assert.True(RolePolicy.VeSoloLoPropio(",Cajero,"));
}
