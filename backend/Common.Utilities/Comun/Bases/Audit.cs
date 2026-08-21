namespace Common.Utilities;

public class Audit
{
    public bool State { get; set; }
    public int CreatedBy { get; set; }
    public DateTime Created { get; set; }
    public int ModifiedBy { get; set; }
    public DateTime Modified { get; set; }
}
/// <remarks>
/// Las marcas de tiempo se toman en UTC, nunca con <c>DateTime.Now</c>.
/// <para>
/// Npgsql no convierte de hora local a UTC al escribir: guarda los dígitos del
/// reloj de pared tal cual, y en una columna <c>timestamptz</c> les pega un
/// <c>+00</c>. Con <c>DateTime.Now</c> en un servidor fuera de UTC, las 12:23 de
/// Bolivia se guardan como "12:23+00" —cuatro horas antes del instante real— y
/// el navegador después las corre otras cuatro al mostrarlas. Que hoy salga bien
/// en producción es solo porque el contenedor corre en UTC; basta levantar la
/// API en una máquina local para que los datos queden mal en silencio.
/// </para>
/// La contrapartida: para "hoy" o "fecha futura" en reglas de negocio esto NO
/// sirve —de noche el día UTC ya cambió respecto al de Bolivia—. Esas
/// comparaciones siguen usando hora local a propósito.
/// </remarks>
public static class AuditHelper
{
    public static void SetCreated<T>(T entidad, int userId) where T : Audit
    {
        DateTime now = DateTime.UtcNow;
        entidad.State = true;
        entidad.Created = now;
        entidad.Modified = now;
        entidad.CreatedBy = userId;
        entidad.ModifiedBy = userId; 
    }
    public static void SetModified<T>(T entidad, int userId) where T : Audit
    {
        DateTime now = DateTime.UtcNow;
        entidad.State = true;
        entidad.Modified = now;
        entidad.ModifiedBy = userId; 
    }
}
