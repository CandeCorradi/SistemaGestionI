using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SistemaGestion.Application.Abstractions;

namespace SistemaGestion.API.Authentication;

/// <summary>
/// Extrae y valida el Firebase ID Token del encabezado Authorization
/// (esquema Bearer) y construye el ClaimsPrincipal con la identidad
/// verificada (AGENTS.md §8: el backend determina la identidad real).
/// </summary>
public sealed class FirebaseAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string BearerPrefix = "Bearer ";

    private readonly IFirebaseAuthService _firebaseAuthService;

    public FirebaseAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IFirebaseAuthService firebaseAuthService)
        : base(options, logger, encoder)
    {
        _firebaseAuthService = firebaseAuthService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var headerValues))
        {
            return AuthenticateResult.NoResult();
        }

        var header = headerValues.ToString();
        if (!header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            Logger.LogWarning(
                "AUTHENTICATION_FAILED: esquema de Authorization inválido en {Path}",
                Request.Path);
            return AuthenticateResult.Fail("Se espera: Authorization: Bearer <firebase-id-token>.");
        }

        var idToken = header[BearerPrefix.Length..].Trim();
        if (string.IsNullOrEmpty(idToken))
        {
            return AuthenticateResult.Fail("El Firebase ID Token está vacío.");
        }

        var result = await _firebaseAuthService.VerifyTokenAsync(idToken, Context.RequestAborted);
        if (result is null)
        {
            Logger.LogWarning(
                "AUTHENTICATION_FAILED: Firebase ID Token inválido o vencido en {Path}",
                Request.Path);
            return AuthenticateResult.Fail("Firebase ID Token inválido o vencido.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.Uid),
            new("firebase_uid", result.Uid),
        };

        if (!string.IsNullOrEmpty(result.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, result.Email));
        }

        // Nombre visible del proveedor (Google) si el token lo incluye; si no,
        // el email, que es el identificador que ya usa el sistema.
        var displayName = string.IsNullOrWhiteSpace(result.Name) ? result.Email : result.Name;
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            claims.Add(new Claim(ClaimTypes.Name, displayName));
        }

        claims.Add(new Claim("email_verified", result.EmailVerified ? "true" : "false"));

        var identity = new ClaimsIdentity(claims, Scheme.Name, ClaimTypes.Name, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;

        await Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Type = "https://datatracker.ietf.org/doc/html/rfc7807",
            Detail = "Se requiere un Firebase ID Token válido: Authorization: Bearer <token>.",
        });
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;

        await Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Forbidden",
            Type = "https://datatracker.ietf.org/doc/html/rfc7807",
            Detail = "La identidad autenticada no tiene permisos para la operación solicitada.",
        });
    }
}
