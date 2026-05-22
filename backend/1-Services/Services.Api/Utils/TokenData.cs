using System;
using Common.Utilities.Comun.Bases;
using Sqids;
using CONST = Common.Utilities.Comun.Bases.TokenDataConst;

namespace Services.Api.Utils;

public class TokenData
{

        public static DataToken GetData(Microsoft.AspNetCore.Http.HttpContext context)
        {
            
            var datos = new DataToken();
            datos.UserId =0;
            datos.Uuid = "";
            datos.SessionId = 0;
            datos.Rol = "";
            datos.ok = true;
            var currentUser = context.User;
        if (currentUser.HasClaim(c => c.Type == CONST.USER_ID))
        {
            var idEncrypt = currentUser.Claims.FirstOrDefault(c => c.Type == CONST.USER_ID)!.Value;
            datos.UserId = Common.Utilities.CustomCryptography.EncondeUserId.DecodeId(idEncrypt);
        }
        if (currentUser.HasClaim(c => c.Type == CONST.UUID))
        {
            datos.Uuid = currentUser.Claims.FirstOrDefault(c => c.Type == CONST.UUID)!.Value.ToString();
        }
        if (currentUser.HasClaim(c => c.Type == CONST.SESSION_ID))
        {
            datos.SessionId = int.Parse(currentUser.Claims.FirstOrDefault(c => c.Type == CONST.SESSION_ID)!.Value);
        }
        if (currentUser.HasClaim(c => c.Type == CONST.ROL))
        {
            datos.Rol = currentUser.Claims.FirstOrDefault(c => c.Type == CONST.ROL)!.Value;
        }
        if (currentUser.HasClaim(c => c.Type == CONST.EMAIL))
        {
            datos.Email = currentUser.Claims.FirstOrDefault(c => c.Type == CONST.EMAIL)!.Value;
        }
            

            //if (datos.IdUsuario == 0 || datos.IdSeguimiento == 0 || datos.CorreoElectronico == "") datos.ok = false;
            if (datos.Uuid == "" || datos.SessionId == 0) datos.ok = false;

            // Datos de la peticion para el seguimiento detalle
            string host = context.Request.Host.Value;
            string ruta_api = context.Request.Path.HasValue ? context.Request.Path.Value : "";
            string ruta_parametros = context.Request.QueryString.HasValue ? context.Request.QueryString.Value ?? "" : "";
            datos.RouteApi = context.Request.IsHttps ? "https://" : "http://" + (host + ruta_api + ruta_parametros);
            datos.Method = context.Request.Method;
            datos.Ip = context.Connection.LocalIpAddress!.ToString();

            return datos;
        }
    public static int GetUserId(Microsoft.AspNetCore.Http.HttpContext context, out bool todoCorrecto)
    {
        todoCorrecto = false;
        int userId = 0;
        try
        {
            var currentUser = context.User;

            if (currentUser.HasClaim(c => c.Type == CONST.USER_ID))
            {
                var idEncrypt = currentUser.Claims.FirstOrDefault(c => c.Type == CONST.USER_ID)!.Value;
                userId = Common.Utilities.CustomCryptography.HashUserId.DecodeId(idEncrypt);
            }
        }
        catch (Exception)
        {
            todoCorrecto = false;
        }
        return userId;
    }
}
