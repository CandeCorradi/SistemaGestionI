using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Common;
using SistemaGestion.Infrastructure.Authentication.Firebase;
using SistemaGestion.Infrastructure.Persistence;
using SistemaGestion.Infrastructure.Persistence.Repositories;
using SistemaGestion.Infrastructure.Storage;

namespace SistemaGestion.Infrastructure;

/// <summary>
/// Registra las dependencias de infraestructura: persistencia (EF Core +
/// PostgreSQL), reloj y Firebase Authentication.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "La cadena de conexión 'DefaultConnection' no está configurada " +
                "(ConnectionStrings__DefaultConnection o DATABASE_CONNECTION_STRING).");
        }

        services.AddDbContext<SistemaGestionDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(SistemaGestionDbContext).Assembly.FullName)));

        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IRolRepository, RolRepository>();
        services.AddScoped<IPermisoRepository, PermisoRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IProveedorRepository, ProveedorRepository>();
        services.AddScoped<IProductoRepository, ProductoRepository>();
        services.AddScoped<IInventarioRepository, InventarioRepository>();
        services.AddScoped<IPresupuestoRepository, PresupuestoRepository>();
        services.AddScoped<IPedidoRepository, PedidoRepository>();
        services.AddScoped<IPagoRepository, PagoRepository>();
        services.AddScoped<IFacturaRepository, FacturaRepository>();
        services.AddScoped<INotificacionRepository, NotificacionRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IReportExporter, Reporting.ReportExporter>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();

        services.AddSingleton<ITimeProvider, SystemTimeProvider>();

        services.AddScoped<IProductoImagenRepository, ProductoImagenRepository>();
        services.AddScoped<ITrabajoRealizadoRepository, TrabajoRealizadoRepository>();
        services.AddScoped<ITrabajoRealizadoImagenRepository, TrabajoRealizadoImagenRepository>();

        services.AddSingleton(_ => new SupabaseStorageOptions
        {
            Url = configuration["SUPABASE_URL"],
            Bucket = configuration["SUPABASE_STORAGE_BUCKET"],
            ServiceRoleKey = configuration["SUPABASE_SERVICE_ROLE_KEY"],
        });

        services.AddSingleton<IStorageService>(sp => new SupabaseStorageService(
            sp.GetRequiredService<SupabaseStorageOptions>(),
            sp.GetRequiredService<ILogger<SupabaseStorageService>>()));

        services.AddOptions<FirebaseOptions>()
            .Bind(configuration.GetSection(FirebaseOptions.SectionName));

        services.AddSingleton<IFirebaseAuthService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<FirebaseOptions>>().Value;
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<FirebaseAuthService>();
            FirebaseConfigurator.TryInitialize(options, logger);
            return new FirebaseAuthService();
        });

        services.AddSingleton<IFirebaseUserService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<FirebaseOptions>>().Value;
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<FirebaseUserService>();
            FirebaseConfigurator.TryInitialize(options, logger);
            return new FirebaseUserService(logger);
        });

        return services;
    }
}