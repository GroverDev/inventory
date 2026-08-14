using Common.Utilities;
using Common.Utilities.Exceptions;
using Seguridad.Domain.Entities.requests;
using Seguridad.Domain.Entities.responses;
using Seguridad.Infrastructure;

namespace Seguridad.Application;

public class AdminApplication(IAdminRepository _adminRepository) : IAdminApplication
{
    public async Task<Response<CreateTenantResponse>> CreateTenant(CreateTenantRequest request, int userId)
    {
        var resp = new Response<CreateTenantResponse>();
        try
        {
            // Operación de plataforma, no de farmacia. SuperAdmin no alcanza: la
            // provisión le crea uno propio a cada farmacia, así que autorizar con ese
            // rol permitiría que cualquier cliente diera de alta clientes nuevos.
            var esAdminPlataforma = await _adminRepository.UserIsPlatformAdmin(userId);
            if (!esAdminPlataforma)
                throw new CustomException("No tiene permisos para dar de alta farmacias.");

            resp.Data = await _adminRepository.CreateTenant(request);
            resp.ok = true;
            resp.SetMessage(MessageTypes.Success,
                $"Farmacia «{resp.Data.Name}» creada. El administrador {resp.Data.AdminEmail} " +
                "deberá cambiar su contraseña al iniciar sesión por primera vez.");
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Soporte Tecnico.", ex); }
        return resp;
    }

    public async Task<Response<bool>> ResetCompany(ResetCompanyRequest request, int userId)
    {
        var resp = new Response<bool>();
        try
        {
            // Primer filtro de autorización (el repositorio vuelve a validar dentro de la transacción).
            var isSuperAdmin = await _adminRepository.UserHasActiveRole(userId, AdminRepository.SuperAdminRole);
            if (!isSuperAdmin)
                throw new CustomException("No tiene permisos para reiniciar la empresa. Se requiere el rol SuperAdmin.");

            var backupPrefix = await _adminRepository.ResetCompany(request, userId);

            resp.Data = true;
            resp.ok = true;
            // Ya no se pide volver a iniciar sesión: el reinicio conserva los usuarios,
            // así que la sesión en curso sigue siendo válida.
            resp.SetMessage(MessageTypes.Success, string.IsNullOrEmpty(backupPrefix)
                ? "Los datos de la farmacia se reiniciaron correctamente (sin respaldo)."
                : $"Los datos de la farmacia se reiniciaron correctamente. Respaldo generado en el esquema «backup» con el prefijo «{backupPrefix}».");
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Soporte Tecnico.", ex); }
        return resp;
    }
}
