namespace Common.Utilities;

public class Audit
{
    public bool State { get; set; }
    public int CreatedBy { get; set; }
    public DateTime Created { get; set; }
    public int ModifiedBy { get; set; }
    public DateTime Modified { get; set; }
}
public static class AuditHelper
{
    public static void SetCreated<T>(T entidad, int userId) where T : Audit
    {
        DateTime now = DateTime.Now;
        entidad.State = true;
        entidad.Created = now;
        entidad.Modified = now;
        entidad.CreatedBy = userId;
        entidad.ModifiedBy = userId; 
    }
    public static void SetModified<T>(T entidad, int userId) where T : Audit
    {
        DateTime now = DateTime.Now;
        entidad.State = true;
        entidad.Modified = now;
        entidad.ModifiedBy = userId; 
    }
}
