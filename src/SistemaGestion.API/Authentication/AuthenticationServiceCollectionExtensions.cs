using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using SistemaGestion.API.Authorization;
using SistemaGestion.Domain.Authorization;

namespace SistemaGestion.API.Authentication;

/// <summary>
/// Registra el esquema de autenticación Firebase (ID Token Bearer) y las
/// políticas de autorización granular: una política "Permission:{codigo}"
/// por cada permiso del catálogo (AGENTS.md §7, roadmap §10.4).
/// </summary>
public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddFirebaseAuthentication(this IServiceCollection services)
    {
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = FirebaseAuthenticationDefaults.Scheme;
                options.DefaultChallengeScheme = FirebaseAuthenticationDefaults.Scheme;
            })
            .AddScheme<AuthenticationSchemeOptions, FirebaseAuthenticationHandler>(
                FirebaseAuthenticationDefaults.Scheme,
                _ => { });

        services.AddAuthorization(options =>
        {
            // Una política por permiso del catálogo. RequireAuthenticatedUser
            // asegura 401 (challenge) sin identidad y 403 (forbid) con
            // identidad pero sin permiso (api-spec §7).
            foreach (var codigo in Permisos.Todos)
            {
                options.AddPolicy(
                    PermissionRequirement.GetPolicyName(codigo),
                    policy =>
                    {
                        policy.RequireAuthenticatedUser();
                        policy.AddRequirements(new PermissionRequirement(codigo));
                    });
            }
        });

        services.AddScoped<IAuthorizationHandler, PermissionHandler>();

        return services;
    }
}
