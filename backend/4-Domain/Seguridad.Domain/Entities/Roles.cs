namespace Seguridad.Domain;

public class Roles:Audit
{
    public int Id { get; set; }

    public string NameRol { get; set; }="";


    public string Description { get; set; }="";

    
}
