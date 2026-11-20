using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SistemaGestion.API;
using SistemaGestion.API.IntegrationTests.Fakes;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Authorization;
using SistemaGestion.Domain.Enums;
using Xunit;

namespace SistemaGestion.API.IntegrationTests;

/// <summary>
/// Tests de API del dashboard (api-spec §19): bloques filtrados por
/// permisos efectivos y bloque de notificaciones propio del token.
/// </summary>
public sealed class DashboardApiTests : IClassFixture<DashboardApiTests.Factory>
{
    private const string AdminToken = "dashboard-admin-token";
    private const string PagosOnlyToken = "dashboard-pagos-token";
    private const string PedidosOnlyToken = "dashboard-pedidos-token";

    private const string AdminUid = "uid-dashboard-admin";
    private const string PagosOnlyUid = "uid-dashboard-pagos";
    private const string PedidosOnlyUid = "uid-dashboard-pedidos";

    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IFirebaseAuthService>(new TokenStubFirebaseAuthService());

                var permisos = new FakePermisoRepository();
                permisos.EfectivosPorUid[AdminUid] = Permisos.Todos.ToList();
                permisos.EfectivosPorUid[PagosOnlyUid] = new List<string> { Permisos.PagosView };
                permisos.EfectivosPorUid[PedidosOnlyUid] = new List<string> { Permisos.PedidosView };
                services.AddSingleton<IPermisoRepository>(permisos);

                var usuarios = new FakeUsuarioRepository();
                usuarios.Seed(AdminUid, "Admin", "Dashboard");
                usuarios.Seed(PagosOnlyUid, "Solo", "Pagos");
                usuarios.Seed(PedidosOnlyUid, "Solo", "Pedidos");
                services.AddSingleton<IUsuarioRepository>(usuarios);

                var dashboard = new FakeDashboardRepository
                {
                    PagosMesMonto = 3000m,
                    PagosMesCantidad = 4,
                    FacturasMesMonto = 8000m,
                    FacturasMesCantidad = 2,
                    PedidosEntregadosMes = 3,
                    Inventario = new DashboardInventarioCounts(
                        ProductosActivos: 10, StockBajo: 1, EnShowroom: 5),
                };
                dashboard.PedidosPorEstado[EstadoPedido.Pendiente] = 2;
                dashboard.PresupuestosPorEstado[EstadoPresupuesto.Enviado] = 6;
                services.AddSingleton<IDashboardRepository>(dashboard);

                var notificaciones = new FakeNotificacionRepository { Unread = 4 };
                services.AddSingleton<INotificacionRepository>(notificaciones);
            });
        }
    }

    private readonly HttpClient _client;

    public DashboardApiTests(Factory factory)
    {
        _client = factory.CreateClient();
    }

    private HttpRequestMessage AuthenticatedRequest(string url, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    [Fact]
    public async Task Get_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithAllPermissions_ReturnsAllBlocks()
    {
        var response = await _client.SendAsync(
            AuthenticatedRequest("/api/v1/dashboard", AdminToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(2, root.GetProperty("orders").GetProperty("pendientes").GetInt32());
        Assert.Equal(3, root.GetProperty("orders").GetProperty("entregadosMes").GetInt32());
        Assert.Equal(6, root.GetProperty("quotes").GetProperty("enviados").GetInt32());
        Assert.Equal(8000m, root.GetProperty("sales").GetProperty("facturadoMes").GetDecimal());
        Assert.Equal(3000m, root.GetProperty("payments").GetProperty("cobradoMes").GetDecimal());
        Assert.Equal(10, root.GetProperty("inventory").GetProperty("productosActivos").GetInt32());
        Assert.Equal(4, root.GetProperty("notifications").GetProperty("unreadCount").GetInt32());
    }

    [Fact]
    public async Task Get_WithOnlyPaymentsPermission_ReturnsOnlyPermittedBlocks()
    {
        var response = await _client.SendAsync(
            AuthenticatedRequest("/api/v1/dashboard", PagosOnlyToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(JsonValueKind.Null, root.GetProperty("orders").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("quotes").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("sales").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, root.GetProperty("payments").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("inventory").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, root.GetProperty("notifications").ValueKind);
    }

    [Fact]
    public async Task Get_WithOnlyOrdersPermission_HidesMoneyBlocks()
    {
        var response = await _client.SendAsync(
            AuthenticatedRequest("/api/v1/dashboard", PedidosOnlyToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.NotEqual(JsonValueKind.Null, root.GetProperty("orders").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("payments").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("sales").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, root.GetProperty("notifications").ValueKind);
    }

    [Fact]
    public async Task Get_WithUnknownInternalUser_ReturnsNotFound()
    {
        var response = await _client.SendAsync(
            AuthenticatedRequest("/api/v1/dashboard", "dashboard-unknown-token"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed class TokenStubFirebaseAuthService : IFirebaseAuthService
    {
        public Task<FirebaseAuthResult?> VerifyTokenAsync(
            string idToken,
            CancellationToken cancellationToken = default)
        {
            FirebaseAuthResult? result = idToken switch
            {
                AdminToken => new FirebaseAuthResult(AdminUid, "admin@test.com", EmailVerified: true),
                PagosOnlyToken => new FirebaseAuthResult(PagosOnlyUid, "pagos@test.com", EmailVerified: true),
                PedidosOnlyToken => new FirebaseAuthResult(PedidosOnlyUid, "pedidos@test.com", EmailVerified: true),
                "dashboard-unknown-token" => new FirebaseAuthResult(
                    "uid-desconocido", "x@test.com", EmailVerified: true),
                _ => null,
            };

            return Task.FromResult(result);
        }
    }
}
