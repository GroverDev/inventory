using Common.Utilities.Bases;
using Common.Utilities.Exceptions;
using Npgsql;

namespace Common.Utilities;

public static class ExceptionHandler
{
    public static Exception HandleException<T>(Exception ex)
    {
        

        // Registro de la excepción (puede ser en un archivo, base de datos, etc.)
        //LogException(ex);

        // Manejo específico de la excepción
        if (ex is PostgresException postgresException)
        {
            return new CustomException("No se puede conectar a la Base de Datos",postgresException, saveLog: true);
        }
        else if (ex is NpgsqlException npgsqlException)
        {
            string message ="";

            if (npgsqlException.InnerException is System.Net.Sockets.SocketException)
            {
                message = "Error de conexión: "+ "Error de conexión: No se puede conectar al servidor PostgreSQL. Verifica que el servidor está en funcionamiento y que la configuración de red es correcta.";
                return new CustomException(message);
                //response.Errors.Add(new BaseError("Npgsql", "Error de conexión: No se puede conectar al servidor PostgreSQL. Verifica que el servidor está en funcionamiento y que la configuración de red es correcta."));
            }
            else if (npgsqlException.Message.Contains("Failed to connect to"))
            {
                message += " | Error de conexión: "+ "Error de conexión: No se puede conectar al servidor PostgreSQL.";
                //response.Errors.Add(new BaseError("Npgsql", "Error de conexión: No se puede conectar al servidor PostgreSQL en 127.0.0.1:5432."));
            }

            message += " | Npgsql Error: "+npgsqlException.Message;
          
            if (npgsqlException.InnerException != null)
            {
                message += " | Npgsql InnerException: "+ npgsqlException.InnerException.Message;
               // response.Errors.Add(new BaseError("Npgsql InnerException",npgsqlException.InnerException.Message));
            }
            return new Exception(message, npgsqlException);
        }
        else if (ex is InvalidOperationException invalidOperationException)
        {
            return new Exception("Invalid Operation: "+invalidOperationException.Message, invalidOperationException);
        }
        else
        {
            return new Exception("General Error:" + ex.Message, ex);
        }
    }

    // private static void LogException(Exception ex)
    // {
    //     // Aquí puedes implementar la lógica para registrar la excepción, por ejemplo, en un archivo de texto
    //     string logFilePath = "exceptions.log";
    //     File.AppendAllText(logFilePath, $"{DateTime.Now}: {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}");
    // }
}
