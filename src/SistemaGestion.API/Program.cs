using System.Linq;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Serilog;
using SistemaGestion.API.Authentication;
using SistemaGestion.API.Authorization;
using SistemaGestion.API.Configuration;
using SistemaGestion.API.Middleware;
using SistemaGestion.API.Seeding;
using SistemaGestion.Application;
using SistemaGestion.Infrastructure;
using SistemaGestion.Infrastructure.Persistence;

// Variables de entorno locales (.env de la raíz, gitignored — AGENTS.md §46).
// Debe ejecutarse ANTES de crear el builder para que la configuración las lea.
var rutaEnvLocal = LocalEnvFileLoader.Load();

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Base de Datos (EF Core + PostgreSQL)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? builder.Configuration.GetConnectionString("PostgresConnection");

builder.Services.AddDbContext<SistemaGestionDbContext>(options =>
    options.UseNpgsql(connectionString));

// Capas y Servicios
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "Firebase ID Token",
            Description = "Firebase ID Token emitido por Firebase Authentication.",
        };
        return Task.CompletedTask;
    });

    // Declara en OpenAPI el permiso granular requerido por cada endpoint
    // (AGENTS.md §49: documentar autorización).
    options.AddOperationTransformer((operation, context, _) =>
    {
        var permisos = context.Description.ActionDescriptor?.EndpointMetadata?
            .OfType<IAuthorizeData>()
            .Select(a => a.Policy)
            .Where(p => p is not null
                && p.StartsWith(PermissionRequirement.PolicyPrefix, StringComparison.Ordinal))
            .Select(p => p![PermissionRequirement.PolicyPrefix.Length..])
            .ToList();

        if (permisos is { Count: > 0 })
        {
            operation.Description = $"Requiere permiso: {string.Join(", ", permisos)}.";
        }

        return Task.CompletedTask;
    });
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFirebaseAuthentication();

// CORS
builder.Services.AddCors(options =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:3000"];

    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(origins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<SistemaGestionDbContext>(name: "postgres");

// Rate limiting (AGENTS.md §45): protege los endpoints públicos
// sensibles (autorregistro) sin afectar al resto de la API.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        await context.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too Many Requests",
                Detail = "Demasiadas solicitudes desde esta dirección. Esperá unos minutos y volvé a intentar.",
            },
            cancellationToken);
    };
    options.AddPolicy<string>("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

// Detrás de un proxy inverso (Render, Azure, etc.) la IP real del cliente
// llega en X-Forwarded-For y el esquema original en X-Forwarded-Proto. Sin
// esto el rate limiting agruparía a todos los clientes bajo la IP del proxy
// (AGENTS.md §45/§56). Se confía en el proxy porque la app no se expone
// directamente a Internet: Render termina TLS en el borde.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownProxies.Clear();
    options.KnownIPNetworks.Clear();
});

var app = builder.Build();

// Debe ejecutarse antes que cualquier middleware que lea Scheme o la IP.
app.UseForwardedHeaders();

if (rutaEnvLocal is not null)
{
    app.Logger.LogInformation("Variables de entorno locales cargadas desde {RutaEnv}", rutaEnvLocal);
}

// Migraciones opcionales al arranque (Render no ofrece preDeployCommand en el
// plan free). Sin try/catch a propósito: si el esquema no se puede actualizar
// el arranque debe fallar de forma visible, no servir una API inconsistente.
if (builder.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var migrationScope = app.Services.CreateScope();
    var dbContext = migrationScope.ServiceProvider.GetRequiredService<SistemaGestionDbContext>();
    await dbContext.Database.MigrateAsync();
    app.Logger.LogInformation("MIGRATIONS_APPLIED: esquema de PostgreSQL actualizado.");
}

// Middlewares del Pipeline HTTP
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var responseDict = report.Entries.ToDictionary(
            entry => entry.Key,
            entry => entry.Value.Status.ToString()
        );

        responseDict["status"] = report.Status.ToString();

        await context.Response.WriteAsync(JsonSerializer.Serialize(responseDict));
    }
});

// Datos ficticios de desarrollo (idempotentes; solo Development,
// AGENTS.md §61: nunca tocar otros ambientes).
if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        await DemoDataSeeder.SeedAsync(scope.ServiceProvider, app.Logger);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "SEED_FAILED: no se pudieron generar los datos ficticios de desarrollo.");
    }
}

app.Run();

/// <summary>
/// Permite que WebApplicationFactory localice la aplicación en los tests.
/// </summary>
public partial class Program { }