namespace Common.Utilities.Comun.Bases;

/// <summary>
/// Regla única que decide si un usuario ve solo sus propios datos o los de toda
/// la farmacia.
/// </summary>
/// <remarks>
/// Un usuario puede tener varios roles: <c>sec.users_roles</c> es muchos a muchos.
/// La regla es <b>gana el más privilegiado</b>: queda restringido solo si
/// <c>Cajero</c> es su <b>único</b> rol.
/// <para>
/// El motivo es que la restricción existe para que un cajero no vea las ventas de
/// otros cajeros. Si el dueño le asignó además un rol administrativo, quitarle la
/// visibilidad porque también atiende caja sería lo contrario de lo que quiso —y
/// en una farmacia chica el dueño suele ser administrador y trabajar el mostrador.
/// </para>
/// Para invertirla (que gane el más restrictivo) alcanza con cambiar
/// <see cref="VeSoloLoPropio"/> por una comprobación de pertenencia.
/// </remarks>
public static class RolePolicy
{
    public const string Cajero = "Cajero";

    /// <summary>
    /// <c>true</c> si el usuario debe ver únicamente sus propias ventas y turnos.
    /// </summary>
    /// <param name="roles">
    /// Roles activos separados por coma, tal como llegan en el claim <c>Roles</c>.
    /// Se acepta también un rol suelto, por compatibilidad con tokens emitidos
    /// antes de este cambio.
    /// </param>
    public static bool VeSoloLoPropio(string? roles)
    {
        if (string.IsNullOrWhiteSpace(roles)) return false;

        var lista = roles
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();

        return lista.Length == 1 && lista[0].Equals(Cajero, StringComparison.OrdinalIgnoreCase);
    }
}
