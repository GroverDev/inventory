namespace Seguridad.Domain;

public class AccessMenu
{
    public AccessMenu()
    {
        // Operaciones = new HashSet<UsuarioOperacionesFormulario>();
        Children = new List<AccessMenu>();
    }

    //public int IdRolFormulario { get; set; }

    public int IdFormulario { get; set; }

    public int? IdFormularioPadre { get; set; }



    public string titulo { get; set; }="";

    public string classIcon { get; set; }="";

    public string classItem { get; set; }="";

    public bool dataToggle { get; set; }
    public int dataTarget { get; set; }

    public string identacion { get; set; }="";

    public string url { get; set; }="";

    public bool SeMuestraEnMenu { get; set; }

    public bool EsFormulario { get; set; }

    // Permisos granulares del formulario para el usuario (unión de sus roles).
    public bool CanCreate { get; set; }
    public bool CanRead { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }

    public List<AccessMenu> Children { get; set; }

}
