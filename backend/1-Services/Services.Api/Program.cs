using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using Dapper;
using Inventory.Application;
using Inventory.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.IdentityModel.Tokens;


using Seguridad.Infrastructure.Extensions;
using Seguridad.Application.Extensions;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Services.Api.jwt;
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

// Injeccion de dependencias Seguridad
builder.Services.AddInjectionSecurityInfraestructure();
builder.Services.AddInjectionSecurityApplication();

// Injeccion de dependencias POS
builder.Services.AddInjectionInventoryApplication();
builder.Services.AddInjectionInventoryInfraestructure();


builder.Services.AddCors(options =>
{
    options.AddPolicy("MisCors",
        builder =>
        {
            builder.WithOrigins("*");
            builder.WithHeaders("*");
            builder.WithMethods("*");

        });

});

var app = builder.Build();

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
app.UseAuthorization();
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