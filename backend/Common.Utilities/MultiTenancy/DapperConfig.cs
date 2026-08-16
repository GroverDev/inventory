using System.Runtime.CompilerServices;
using Dapper;

namespace Common.Utilities;

/// <summary>
/// Configuración de Dapper común a todo el sistema.
/// </summary>
/// <remarks>
/// <para>
/// Las tablas usan snake_case y las propiedades PascalCase, así que sin
/// <see cref="DefaultTypeMap.MatchNamesWithUnderscores"/> ninguna columna con
/// guion bajo mapea. Lo grave es CÓMO falla: Dapper no lanza, deja la propiedad
/// en su valor por defecto. Una consulta devuelve objetos a medio llenar —Guid
/// vacíos, cadenas vacías, ceros— y nada avisa.
/// </para>
/// <para>
/// Vivía en <c>Program.cs</c> de la API, y por eso solo se aplicaba cuando quien
/// corría los repositorios era el servicio web. Cualquier otro consumidor —las
/// pruebas, un proceso de fondo, una herramienta de línea de comandos— usaba los
/// mismos repositorios con Dapper sin configurar. Se detectó porque una prueba
/// del repositorio de trazabilidad recibía el identificador de la venta en
/// <c>Guid.Empty</c>.
/// </para>
/// <para>
/// Con <c>ModuleInitializer</c> se aplica al cargarse este ensamblado, que es
/// referencia de todos los demás: ya no depende de que alguien se acuerde de
/// llamarlo desde su punto de entrada.
/// </para>
/// </remarks>
public static class DapperConfig
{
    [ModuleInitializer]
    internal static void Configurar()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }
}
