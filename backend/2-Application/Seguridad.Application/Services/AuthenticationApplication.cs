using Common.Utilities;
using Common.Utilities.Exceptions;
using Seguridad.Domain;
using Seguridad.Infrastructure;

namespace Seguridad.Application;

public class AuthenticationApplication(IAuthenticationRepository _authenticationRepository) : IAuthenticationApplication
{
    public async Task<Response<LoginResponse>> Login(LoginRequest login)
    {
        var resp = new Response<LoginResponse>() { Data = new LoginResponse() };
        try
        {
            resp.Data = await _authenticationRepository.Login(login);
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Soporte Técnico.", ex); }

        return resp;
    }
}
