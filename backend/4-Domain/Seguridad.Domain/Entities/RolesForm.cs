namespace Seguridad.Domain;

public class RolesForm : Audit
{
    public int Id { get; set; }
    public int RolId { get; set; }
    public int FormId { get; set; }
}
