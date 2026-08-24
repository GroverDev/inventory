using System.Data;
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
/// llamarlo desde su punto de entrada. La API lo carga al arrancar, así que ahí
/// siempre está configurado antes de la primera consulta.
/// </para>
/// <para>
/// <see cref="Configurar"/> es pública para el caso en que ese ensamblado todavía
/// no se haya cargado —una prueba que abre una conexión suelta, sin pasar por
/// ningún tipo de este proyecto—: llamarla es idempotente y garantiza que lo que
/// se ejercita es esta configuración y no una copia escrita en el fixture.
/// </para>
/// </remarks>
public static class DapperConfig
{
    [ModuleInitializer]
    public static void Configurar()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
    }

    /// <summary>
    /// Dapper 2.1.35 no sabe pasar <see cref="DateOnly"/> como parámetro: lanza
    /// "cannot be used as a parameter value" apenas una consulta recibe uno. Las
    /// fechas de compra son días del calendario y viven en columnas <c>date</c>,
    /// así que en C# son DateOnly (ver PurchaseRepository); sin este handler,
    /// listar o guardar un pedido falla.
    /// </summary>
    /// <remarks>
    /// Dapper registra también la variante anulable, así que cubre
    /// <c>DateOnly?</c> sin declararla aparte.
    /// </remarks>
    private sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override void SetValue(IDbDataParameter parameter, DateOnly value)
        {
            parameter.DbType = DbType.Date;
            parameter.Value = value.ToDateTime(TimeOnly.MinValue);
        }

        // Npgsql devuelve DateOnly para las columnas date, pero la lectura puede
        // llegar como DateTime o texto según de dónde venga el valor.
        public override DateOnly Parse(object value) => value switch
        {
            DateOnly d => d,
            DateTime dt => DateOnly.FromDateTime(dt),
            string s => DateOnly.Parse(s),
            _ => throw new InvalidCastException(
                $"No se puede convertir {value?.GetType().Name ?? "null"} a DateOnly."),
        };
    }
}
