using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Common.Utilities;
using Dapper;
using Inventory.Application;
using Inventory.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Services.Api;


using Seguridad.Infrastructure.Extensions;
using Seguridad.Application.Extensions;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Services.Api.jwt;
using Services.Api.Middleware;
using Services.Api.Security;
using Sqids;



DefaultTypeMap.MatchNamesWithUnderscores = true;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, lc) =>
{
    lc.ReadFrom.Configuration(ctx.Configuration)
    .Enrich.With(new CuidEnricher())
    .Enrich.FromLogContext();
});


var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<Seguridad.Domain.MfaSettings>(builder.Configuration.GetSection("MfaSettings"));
builder.Services.Configure<Seguridad.Domain.LoginSettings>(builder.Configuration.GetSection("LoginSettings"));
builder.Services.Configure<Inventory.Application.PosSettings>(builder.Configuration.GetSection("PosSettings"));
builder.Services.AddControllers();
builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null; // Para mantener el casing original de las propiedades
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull; // Ignorar valores null

            });

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,//true,
        ValidateAudience = false, //true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        //ValidIssuer = jwtSettings.Issuer,
        //ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings!.Secret)),
    };
});

builder.Services.AddAuthorization();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(
    c =>
    {
        // Crear grupos de documentos Swagger por controlador/grupo
        c.SwaggerDoc("SIAT", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "API del Sistema Integrado de Administración Tributaria (SIAT)",
            Version = "v1"
        });

        c.SwaggerDoc("POS", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "API de Punto de Venta (POS)",
            Version = "v1"
        });

        c.SwaggerDoc("SECURITY", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "API de Seguridad (SECURITY)",
            Version = "v1"
        });

        // Configurar que cada acción sea incluida en el grupo basado en el controlador (GroupName de ApiExplorerSettings)
        c.DocInclusionPredicate((docName, apiDesc) =>
        {
            var groupName = apiDesc.GroupName;
            return groupName == docName;
        });
    }
);

// Tenant del request. Scoped: una instancia por llamada, poblada por
// TenantResolutionMiddleware a partir del claim del JWT.
builder.Services.AddScoped<Common.Utilities.MultiTenancy.ITenantContext,
                           Common.Utilities.MultiTenancy.TenantContext>();

// Injeccion de dependencias Seguridad
builder.Services.AddInjectionSecurityInfraestructure();
builder.Services.AddInjectionSecurityApplication();

// Injeccion de dependencias POS
builder.Services.AddInjectionInventoryApplication();
builder.Services.AddInjectionInventoryInfraestructure();


// Orígenes permitidos por CORS. Se configuran en appsettings (sección "Cors").
// Si la lista queda vacía ningún navegador podrá consumir la API: es
// intencional, evita volver a exponerla a cualquier origen por descuido.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("MisCors",
        policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  // Necesario para que el navegador envíe la cookie del
                  // refresh token. Exige orígenes explícitos, nunca "*".
                  .AllowCredentials();
        });

});

// Cloudflare Turnstile. El captcha se exige a los navegadores parados en
// nuestra propia web, identificados por su Origin, así que reutiliza la lista
// de CORS: una sola definición de "cuál es nuestra web".
builder.Services.Configure<TurnstileSettings>(builder.Configuration.GetSection("Turnstile"));
builder.Services.PostConfigure<TurnstileSettings>(o => o.WebOrigins = allowedOrigins);
// Singleton: el estado del circuito es compartido por todas las peticiones.
builder.Services.AddSingleton<TurnstileCircuitBreaker>();
builder.Services.AddHttpClient<ITurnstileValidator, TurnstileValidator>(client =>
{
    // El login no puede quedar esperando a un servicio externo. Si Cloudflare
    // tarda, se corta y el corte cuenta como falla para el circuit breaker.
    client.Timeout = TimeSpan.FromSeconds(5);
});

// Detrás de un proxy inverso (nginx) la IP del cliente llega en X-Forwarded-For.
// Sin esto todas las peticiones parecerían venir del proxy y el límite por IP
// castigaría a todos los usuarios por igual. Solo se confía en la cabecera si
// la petición viene de un proxy declarado: de lo contrario cualquiera podría
// falsear su IP de origen.
var knownProxies = builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
var trustAllProxies = builder.Configuration.GetValue<bool>("ForwardedHeaders:TrustAllProxies");

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;

    if (trustAllProxies)
    {
        // Necesario en Docker: nginx corre en el host y alcanza al contenedor
        // por el gateway de la red, cuya IP es dinámica y no es loopback, así
        // que no se puede declarar de antemano.
        //
        // Solo activar cuando el puerto de la API no sea accesible desde fuera
        // (aquí se publica como 127.0.0.1:6001, de modo que únicamente nginx
        // llega). Si la API quedara expuesta, cualquiera podría falsear su IP
        // de origen mediante X-Forwarded-For.
        options.KnownProxies.Clear();
        options.KnownNetworks.Clear();
    }
    else
    {
        foreach (var proxy in knownProxies)
        {
            if (IPAddress.TryParse(proxy, out var ip)) options.KnownProxies.Add(ip);
        }
    }
});

// El cuerpo del 429 respeta el envoltorio Response<T> que esperan la web y el
// móvil; WriteAsJsonAsync usaría camelCase por defecto y rompería el parseo.
var envelopeJsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = null,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

var loginPermitLimit = builder.Configuration.GetValue<int?>("RateLimiting:LoginPermitLimit") ?? 10;
var loginWindowMinutes = builder.Configuration.GetValue<int?>("RateLimiting:LoginWindowMinutes") ?? 1;

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(RateLimitPolicies.Login, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = loginPermitLimit,
                Window = TimeSpan.FromMinutes(loginWindowMinutes),
                QueueLimit = 0
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        var body = new Response<string>();
        body.SetMessage(MessageTypes.Warning,
            "Demasiados intentos desde esta red. Espera un momento e inténtalo nuevamente.");

        await context.HttpContext.Response.WriteAsJsonAsync(
            body, envelopeJsonOptions, cancellationToken);
    };
});

var app = builder.Build();

// Debe ir antes que cualquier middleware que lea la IP o el esquema.
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        // Apuntar a cada grupo de controladores
        c.SwaggerEndpoint("/swagger/SIAT/swagger.json", "SIAT - API");
        c.SwaggerEndpoint("/swagger/POS/swagger.json", "POS - API");
        c.SwaggerEndpoint("/swagger/SECURITY/swagger.json", "SECURITY - API");
    });
}
app.UseCors("MisCors");
app.UseHttpsRedirection();
app.UseAuthentication();
// Después de UseAuthentication: necesita los claims ya resueltos para leer el
// tenant. Antes de los controladores, que abren conexiones a la base.
app.UseTenantResolution();
app.UseAuthorization();
// Después de UseCors para que la respuesta 429 lleve las cabeceras CORS y el
// navegador pueda leer el mensaje en vez de reportar un error de red.
app.UseRateLimiter();
#region
var culture = CultureInfo.CreateSpecificCulture("es-BO");
var dateformat = new DateTimeFormatInfo
{
    ShortDatePattern = "dd/MM/yyyy",
    LongDatePattern = "dd/MM/yyyy HH:mm:ss"
};
culture.DateTimeFormat = dateformat;

var supportedCultures = new[] { culture };

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});
#endregion

app.MapControllers();
app.Run();



public class CuidEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var uuid = Guid.NewGuid();
        //logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("cuid", uuid));
        var enrichProperty = propertyFactory
                .CreateProperty(
                    "Cuid",
                    uuid);

        logEvent.AddOrUpdateProperty(enrichProperty);
    }
}