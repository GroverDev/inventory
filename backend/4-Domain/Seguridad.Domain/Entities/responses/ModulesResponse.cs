namespace Seguridad.Domain;

public class ModulesResponse
{
    public int Id { get; set; }
    public string NameModule { get; set; } = "";
    public int ShowOrder { get; set; }
    public string Route { get; set; } = "";
    public string IconCss { get; set; } = "";
    public bool State { get; set; }
}
