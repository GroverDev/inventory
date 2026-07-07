using Common.Utilities;
using Common.Utilities.Exceptions;
using Seguridad.Domain.Entities.requests;
using Seguridad.Infrastructure;

namespace Seguridad.Application;

public class AdminApplication(IAdminRepository _adminRepository) : IAdminApplication
{
    public async Task<Response<bool>> ResetCompany(ResetCompanyRequest request, int userId)
    {
        var resp = new Response<bool>();
        try
        {
            // Primer filtro de autorización (el repositorio vuelve a validar dentro de la transacción).
            var isSuperAdmin = await _adminRepository.UserHasActiveRole(userId, AdminRepository.SuperAdminRole);
            if (!isSuperAdmin)
                throw new CustomException("No tiene permisos para reiniciar la empresa. Se requiere el rol SuperAdmin.");

            var backupSchema = await _adminRepository.ResetCompany(request, userId);

            resp.Data = true;
            resp.ok = true;
            resp.SetMessage(MessageTypes.Success, string.IsNullOrEmpty(backupSchema)
                ? "La empresa se reinició correctamente (sin respaldo). Vuelva a iniciar sesión con el nuevo administrador."
                : $"La empresa se reinició correctamente. Respaldo generado en el esquema «{backupSchema}». Vuelva a iniciar sesión con el nuevo administrador.");
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Soporte Tecnico.", ex); }
        return resp;
    }
}
