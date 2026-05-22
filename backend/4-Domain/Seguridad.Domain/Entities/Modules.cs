using System;

namespace Seguridad.Domain.Entities;

public class Modules: Audit
{
    public int Id { get; set; }
    public string NameModule { get; set; } = "";
    public int ShowOrder { get; set; }
    public string Route { get; set; } = "";
    public string IconCss { get; set; } = "";
    
}
