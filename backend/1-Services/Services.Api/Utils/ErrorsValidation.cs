using Common.Utilities;
using FluentValidation.Results;

namespace Services.Api.Utils;

public class ErrorsValidation<T> where T : new()
{
    public static Response<T> GetResponse(List<ValidationFailure> errors)
    {
        string mensaje = "";
        foreach (var error in errors)
        {
            mensaje += error.ErrorMessage + " ";
        }
        mensaje = mensaje.Trim();

        var objResp = new Response<T>
        {
            Data = new T(),
            ok = false
        };
        objResp.SetMessage(MessageTypes.Error, mensaje);
        return objResp;
    }
}

public class ErrorsValidationString
{

    public static Response<string> GetResponseString(List<ValidationFailure> errors)
    {
        string mensaje = "";
        foreach (var error in errors)
        {
            mensaje += error.ErrorMessage + " ";
        }
        mensaje = mensaje.Trim();

        var objResp = new Response<string>
        {
            Data = "",
            ok = false
        };
        objResp.SetMessage(MessageTypes.Error, mensaje);
        return objResp;
    }

}