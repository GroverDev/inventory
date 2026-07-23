using Common.Utilities;
using Common.Utilities.Exceptions;
using Seguridad.Domain;
using Seguridad.Infrastructure;

namespace Seguridad.Application;

public class AccessMenuApplication(
    IFormsRepository _formsRepository,
    IRolesRepository _rolesRepository
) : IAccessMenuApplication
{
    public async Task<Response<List<AccessMenu>>> GetAccesMenuXUserId(int userId)
    {
        var objRespuesta = new Response<List<AccessMenu>>() { Data = [] };
        try
        {
            var lista = new List<Forms>();
            lista = await ListaMenuDeAccesos(userId);

            var listaRest = new List<AccessMenu>();
            foreach (var acceso in lista)
            {

                AccessMenu accesoNg = new()
                {
                    IdFormulario = acceso.Id,
                    IdFormularioPadre = acceso.FormId,
                    titulo = acceso.NameForm,
                    classIcon = acceso.IconCss,
                    classItem = "nav-link link",
                    dataToggle = false,
                    dataTarget = acceso.Id,
                    identacion = "",
                    url = acceso.Route,
                    SeMuestraEnMenu = acceso.ShowMenu,
                    EsFormulario = acceso.IsFormRegister,
                    CanCreate = acceso.CanCreate,
                    CanRead = acceso.CanRead,
                    CanUpdate = acceso.CanUpdate,
                    CanDelete = acceso.CanDelete
                };
                listaRest.Add(accesoNg);
            }
            objRespuesta.Data = listaRest;
            objRespuesta.ok = true;
        }
        catch (CustomException ex)
        {
            objRespuesta.SetMessage(MessageTypes.Error, ex.Message);
        }
        catch (Exception ex)
        {
            objRespuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese Soporte Tecnico", ex);
        }

        return objRespuesta;
    }
    private async Task<List<Forms>> ListaMenuDeAccesos(int idUsuarioSession)
    {

        var listaUsuariosFormularios = new List<Forms>();

        var listaRolesUsuario = new List<Roles>();

        try
        {
            listaRolesUsuario = await _rolesRepository.GetRolesXUserId(idUsuarioSession);


            foreach (var rolUsuario in listaRolesUsuario)
            {
                var listaFormulariosTemporales = await _formsRepository.GetFormsXRolId(rolUsuario.Id);
                listaUsuariosFormularios = listaUsuariosFormularios.Concat(listaFormulariosTemporales).ToList();
            }
            listaUsuariosFormularios = remueveDuplicados(listaUsuariosFormularios);
            listaUsuariosFormularios = listaUsuariosFormularios.OrderBy(x => x.ShowOrder).ToList();
        }
        catch (CustomException ex)
        {
            throw new CustomException(ex.Message, ex);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message, ex);
        }
        return listaUsuariosFormularios;
    }

    private static List<Forms> remueveDuplicados(List<Forms> listaFormConDuplicados)
    {
        var listaFormSinDuplicados = new List<Forms>();
        Dictionary<int, Forms> codigosUnicos = new Dictionary<int, Forms>();
        foreach (var formu in listaFormConDuplicados)
        {
            if (!codigosUnicos.ContainsKey(formu.Id))
            {
                codigosUnicos.Add(formu.Id, formu);
                listaFormSinDuplicados.Add(formu);
            }
            else
            {
                // El usuario tiene el mismo formulario en varios roles:
                // el permiso efectivo es la unión (OR) de los permisos de cada rol.
                var existente = codigosUnicos[formu.Id];
                existente.CanCreate |= formu.CanCreate;
                existente.CanRead   |= formu.CanRead;
                existente.CanUpdate |= formu.CanUpdate;
                existente.CanDelete |= formu.CanDelete;
            }
        }
        return listaFormSinDuplicados;
    }

}
