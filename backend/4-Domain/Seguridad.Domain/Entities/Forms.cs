namespace Seguridad.Domain;

public class Forms : Audit
{
    public int Id { get; set; }
    public int FormId { get; set; }
    public string NameForm { get; set; } = "";
    public string Description { get; set; } = "";
    public string IconCss { get; set; } = "";
    public int ShowOrder { get; set; }
    public string Route { get; set; } = "";
    public bool ShowMenu { get; set; }
    public bool IsFormRegister { get; set; }
    public int ModuleId { get; set; }
    public string Controller { get; set; } = "";

    // Permisos granulares por rol+formulario (provienen de sec.roles_forms).
    // Default true: si no están definidos (NULL en BD) se asume acceso total, por compatibilidad.
    public bool CanCreate { get; set; } = true;
    public bool CanRead { get; set; } = true;
    public bool CanUpdate { get; set; } = true;
    public bool CanDelete { get; set; } = true;
}
