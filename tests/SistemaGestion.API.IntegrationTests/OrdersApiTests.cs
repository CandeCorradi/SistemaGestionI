using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
/// Tests de API de /api/v1/orders (api-spec §16): autenticación,
/// autorización granular (VIEW/UPDATE/CANCEL), detalle con snapshot
/// (BR-071), historial (BR-075), transiciones validadas (BR-072) y edición
/// de seguimiento (PATCH, BR-076). Los pedidos se crean aceptando un
/// presupuesto (BR-065).
/// </summary>
public sealed class OrdersApiTests : IClassFixture<OrdersApiTests.Factory>
{
    private const string AdminToken = "orders-admin-token";
    private const string ViewToken = "orders-view-token";
    private const string UpdateToken = "orders-update-token";
    private const string CancelToken = "orders-cancel-token";
    private const string NoPermisoToken = "orders-nopermiso-token";

    private const string AdminUid = "uid-orders-admin";
    private const string ViewUid = "uid-orders-view";
    private const string UpdateUid = "uid-orders-update";
    private const string CancelUid = "uid-orders-cancel";
    private const string NoPermisoUid = "uid-orders-nopermiso";

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
                permisos.EfectivosPorUid[ViewUid] = new List<string>
                {
                    Permisos.PedidosView,
                };
                permisos.EfectivosPorUid[UpdateUid] = new List<string>
                {
                    Permisos.PedidosView, Permisos.PedidosUpdate,
                };
                permisos.EfectivosPorUid[CancelUid] = new List<string>
                {
                    Permisos.PedidosView, Permisos.PedidosCancel,
                };
                permisos.EfectivosPorUid[NoPermisoUid] = new List<string>
                {
                    Permisos.UsuariosView,
                };
                services.AddSingleton<IPermisoRepository>(permisos);

                var clientes = new FakeClienteRepository();
                services.AddSingleton<IClienteRepository>(clientes);

                var productos = new FakeProductoRepository();
                services.AddSingleton<IProductoRepository>(productos);

                var usuarios = new FakeUsuarioRepository();
                usuarios.Seed(AdminUid, "Admin", "Sistema");
                usuarios.Seed(ViewUid, "Ver", "Pedidos");
                usuarios.Seed(UpdateUid, "Edita", "Pedidos");
                usuarios.Seed(CancelUid, "Anula", "Pedidos");
                services.AddSingleton<IUsuarioRepository>(usuarios);

                var pedidos = new FakePedidoRepository(usuarios);
                services.AddSingleton<IPedidoRepository>(pedidos);
                services.AddSingleton<IPresupuestoRepository>(
                    new FakePresupuestoRepository(clientes, pedidos));
            });
        }
    }

    private readonly Factory _factory;

    private readonly HttpClient _client;

    public OrdersApiTests(Factory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private FakeClienteRepository Clientes
        => (FakeClienteRepository)_factory.Services.GetRequiredService<IClienteRepository>();

    private HttpRequestMessage AuthenticatedRequest(HttpMethod method, string url, string token = AdminToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    /// <summary>Crea, envía y acepta un presupuesto vía API para sembrar un pedido.</summary>
    private async Task<int> SeedAcceptedOrderAsync()
    {
        var cliente = Clientes.Seed($"order-{Guid.NewGuid():N}@test.com", "Orden", "Prueba");

        var create = AuthenticatedRequest(HttpMethod.Post, "/api/v1/quotes");
        create.Content = JsonContent.Create(new
        {
            customerId = cliente.Id,
            expirationDate = (string?)null,
            notes = (string?)null,
            conditions = (string?)null,
            items = new[]
            {
                new { productId = (int?)null, description = "Mueble a medida", quantity = 1, unitPrice = 250m, discount = (decimal?)null },
            },
        });
        using var creado = JsonDocument.Parse(
            await (await _client.SendAsync(create)).Content.ReadAsStringAsync());
        var quoteId = creado.RootElement.GetProperty("id").GetInt32();

        await _client.SendAsync(AuthenticatedRequest(HttpMethod.Post, $"/api/v1/quotes/{quoteId}/send"));

        using var aceptado = JsonDocument.Parse(
            await (await _client.SendAsync(
                AuthenticatedRequest(HttpMethod.Post, $"/api/v1/quotes/{quoteId}/accept")))
                .Content.ReadAsStringAsync());
        return aceptado.RootElement.GetProperty("orderId").GetInt32();
    }

    private async Task<JsonElement> GetJsonAsync(string url, string token = ViewToken)
    {
        var response = await _client.SendAsync(AuthenticatedRequest(HttpMethod.Get, url, token));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    [Fact]
    public async Task List_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_WithoutPedidosView_ReturnsForbidden()
    {
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/orders", NoPermisoToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task List_WithView_ReturnsOrderWithSnapshotTotal_Br071()
    {
        var orderId = await SeedAcceptedOrderAsync();

        var body = await GetJsonAsync("/api/v1/orders");

        var item = Assert.Single(
            body.GetProperty("items").EnumerateArray().ToList(),
            i => i.GetProperty("id").GetInt32() == orderId);
        Assert.Equal(250m, item.GetProperty("total").GetDecimal());
        Assert.Equal(0, item.GetProperty("status").GetInt32());
        Assert.Equal("Orden Prueba", item.GetProperty("customerName").GetString());
        Assert.True(item.GetProperty("quoteId").GetInt32() > 0);
    }

    [Fact]
    public async Task List_FilterByStatus_ReturnsOnlyMatch()
    {
        // El factory es compartido entre tests: se comparan sólo los pedidos
        // sembrados por este test (A confirmado, B pendiente).
        var pedidoA = await SeedAcceptedOrderAsync();
        var pedidoB = await SeedAcceptedOrderAsync();

        var confirm = AuthenticatedRequest(HttpMethod.Post, $"/api/v1/orders/{pedidoA}/status");
        confirm.Content = JsonContent.Create(new { status = 1, comment = (string?)null });
        var confirmResponse = await _client.SendAsync(confirm);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        var confirmados = (await GetJsonAsync("/api/v1/orders?status=1"))
            .GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt32()).ToList();
        Assert.Contains(pedidoA, confirmados);
        Assert.DoesNotContain(pedidoB, confirmados);

        var pendientes = (await GetJsonAsync("/api/v1/orders?status=0"))
            .GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt32()).ToList();
        Assert.Contains(pedidoB, pendientes);
        Assert.DoesNotContain(pedidoA, pendientes);
    }

    [Fact]
    public async Task GetById_Existing_ReturnsItemsFromQuoteSnapshot()
    {
        var orderId = await SeedAcceptedOrderAsync();

        var body = await GetJsonAsync($"/api/v1/orders/{orderId}");

        Assert.Equal(orderId, body.GetProperty("id").GetInt32());
        var item = Assert.Single(body.GetProperty("items").EnumerateArray());
        Assert.Equal("Mueble a medida", item.GetProperty("description").GetString());
        Assert.Equal(250m, item.GetProperty("unitPrice").GetDecimal());
    }

    [Fact]
    public async Task GetById_Unknown_ReturnsNotFound()
    {
        var response = await _client.SendAsync(
            AuthenticatedRequest(HttpMethod.Get, "/api/v1/orders/9999"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task History_ReturnsCreationRecordAndTransition_Br075()
    {
        var orderId = await SeedAcceptedOrderAsync();
        var confirm = AuthenticatedRequest(HttpMethod.Post, $"/api/v1/orders/{orderId}/status");
        confirm.Content = JsonContent.Create(new { status = 1, comment = "Confirmado con el cliente" });
        await _client.SendAsync(confirm);

        var body = await GetJsonAsync($"/api/v1/orders/{orderId}/history");

        var filas = body.EnumerateArray().ToList();
        Assert.Equal(2, filas.Count);

        // Orden: el más reciente primero.
        Assert.Equal(0, filas[0].GetProperty("previousStatus").GetInt32());
        Assert.Equal(1, filas[0].GetProperty("newStatus").GetInt32());
        Assert.Equal("Confirmado con el cliente", filas[0].GetProperty("comment").GetString());
        Assert.Equal("Admin Sistema", filas[0].GetProperty("userName").GetString());

        Assert.Null(filas[1].GetProperty("previousStatus").GetString());
        Assert.Equal(0, filas[1].GetProperty("newStatus").GetInt32());
        Assert.Null(filas[1].GetProperty("userName").GetString());
    }

    [Fact]
    public async Task History_UnknownOrder_ReturnsNotFound()
    {
        var response = await _client.SendAsync(
            AuthenticatedRequest(HttpMethod.Get, "/api/v1/orders/9999/history"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Status_PendienteToConfirmado_WithUpdatePermission_Ok()
    {
        var orderId = await SeedAcceptedOrderAsync();

        var request = AuthenticatedRequest(
            HttpMethod.Post, $"/api/v1/orders/{orderId}/status", UpdateToken);
        request.Content = JsonContent.Create(new { status = 1, comment = (string?)null });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, body.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Status_InvalidJump_ReturnsConflict_Br072()
    {
        var orderId = await SeedAcceptedOrderAsync();

        var request = AuthenticatedRequest(HttpMethod.Post, $"/api/v1/orders/{orderId}/status");
        request.Content = JsonContent.Create(new { status = 3, comment = (string?)null });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Status_Cancel_WithoutCancelPermission_ReturnsForbidden()
    {
        var orderId = await SeedAcceptedOrderAsync();

        var request = AuthenticatedRequest(
            HttpMethod.Post, $"/api/v1/orders/{orderId}/status", UpdateToken);
        request.Content = JsonContent.Create(new { status = 5, comment = (string?)null });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Status_Cancel_WithCancelPermission_Ok()
    {
        var orderId = await SeedAcceptedOrderAsync();

        var request = AuthenticatedRequest(
            HttpMethod.Post, $"/api/v1/orders/{orderId}/status", CancelToken);
        request.Content = JsonContent.Create(new { status = 5, comment = "Se anuló" });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(5, body.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Status_UndefinedValue_ReturnsBadRequest()
    {
        var orderId = await SeedAcceptedOrderAsync();

        var request = AuthenticatedRequest(HttpMethod.Post, $"/api/v1/orders/{orderId}/status");
        request.Content = JsonContent.Create(new { status = 99, comment = (string?)null });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Status_UnknownOrder_ReturnsNotFound()
    {
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/orders/9999/status");
        request.Content = JsonContent.Create(new { status = 1, comment = (string?)null });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ------------------------------------------------------------------
    // Edición de seguimiento (PATCH, BR-076)
    // ------------------------------------------------------------------

    [Fact]
    public async Task Update_WithUpdatePermission_PersistsNotesAndEstimate_Br076()
    {
        var orderId = await SeedAcceptedOrderAsync();
        var estimada = DateTime.UtcNow.Date.AddDays(7);

        var request = AuthenticatedRequest(
            HttpMethod.Patch, $"/api/v1/orders/{orderId}", UpdateToken);
        request.Content = JsonContent.Create(new
        {
            notes = "  Entrega coordinada  ",
            estimatedDeliveryDate = estimada.ToString("yyyy-MM-dd"),
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Entrega coordinada", body.RootElement.GetProperty("notes").GetString());
        Assert.Equal(
            DateTimeKind.Utc,
            body.RootElement.GetProperty("estimatedDeliveryDate").GetDateTime().Kind);

        var detalle = await GetJsonAsync($"/api/v1/orders/{orderId}");
        Assert.Equal("Entrega coordinada", detalle.GetProperty("notes").GetString());
        Assert.Equal(
            estimada,
            detalle.GetProperty("estimatedDeliveryDate").GetDateTime().Date);
    }

    [Fact]
    public async Task Update_WithoutToken_ReturnsUnauthorized()
    {
        var orderId = await SeedAcceptedOrderAsync();
        var request = new HttpRequestMessage(
            HttpMethod.Patch, $"/api/v1/orders/{orderId}")
        {
            Content = JsonContent.Create(new { notes = "x", estimatedDeliveryDate = (string?)null }),
        };

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithOnlyViewPermission_ReturnsForbidden()
    {
        var orderId = await SeedAcceptedOrderAsync();
        var request = AuthenticatedRequest(
            HttpMethod.Patch, $"/api/v1/orders/{orderId}", ViewToken);
        request.Content = JsonContent.Create(new { notes = "x", estimatedDeliveryDate = (string?)null });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_UnknownOrder_ReturnsNotFound()
    {
        var request = AuthenticatedRequest(HttpMethod.Patch, "/api/v1/orders/9999");
        request.Content = JsonContent.Create(new { notes = "x", estimatedDeliveryDate = (string?)null });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_TooLongNotes_ReturnsBadRequest()
    {
        var orderId = await SeedAcceptedOrderAsync();
        var request = AuthenticatedRequest(HttpMethod.Patch, $"/api/v1/orders/{orderId}");
        request.Content = JsonContent.Create(new
        {
            notes = new string('x', 2001),
            estimatedDeliveryDate = (string?)null,
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_TerminalOrder_ReturnsConflict_Br074()
    {
        var orderId = await SeedAcceptedOrderAsync();

        // Ciclo completo: Pendiente → ... → Completado (6).
        foreach (var status in new[] { 1, 2, 3, 4, 6 })
        {
            var paso = AuthenticatedRequest(HttpMethod.Post, $"/api/v1/orders/{orderId}/status");
            paso.Content = JsonContent.Create(new { status, comment = (string?)null });
            Assert.Equal(
                HttpStatusCode.OK,
                (await _client.SendAsync(paso)).StatusCode);
        }

        var request = AuthenticatedRequest(HttpMethod.Patch, $"/api/v1/orders/{orderId}");
        request.Content = JsonContent.Create(new { notes = "x", estimatedDeliveryDate = (string?)null });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Detail_AfterProductionTransitions_ReturnsTrackingDates_Br076()
    {
        var orderId = await SeedAcceptedOrderAsync();

        foreach (var status in new[] { 1, 2 })
        {
            var paso = AuthenticatedRequest(HttpMethod.Post, $"/api/v1/orders/{orderId}/status");
            paso.Content = JsonContent.Create(new { status, comment = (string?)null });
            Assert.Equal(
                HttpStatusCode.OK,
                (await _client.SendAsync(paso)).StatusCode);
        }

        var detalle = await GetJsonAsync($"/api/v1/orders/{orderId}");

        Assert.Equal(
            JsonValueKind.String,
            detalle.GetProperty("productionStartDate").ValueKind);
        Assert.Equal(
            JsonValueKind.Null,
            detalle.GetProperty("productionEndDate").ValueKind);
        Assert.Equal(
            JsonValueKind.Null,
            detalle.GetProperty("deliveryDate").ValueKind);
    }

    [Fact]
    public void EstadoPedido_SerializesAsNumber()
    {
        Assert.Equal(0, (int)EstadoPedido.Pendiente);
        Assert.Equal(1, (int)EstadoPedido.Confirmado);
        Assert.Equal(2, (int)EstadoPedido.EnProduccion);
        Assert.Equal(3, (int)EstadoPedido.Listo);
        Assert.Equal(4, (int)EstadoPedido.Entregado);
        Assert.Equal(5, (int)EstadoPedido.Cancelado);
        Assert.Equal(6, (int)EstadoPedido.Completado);
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
                ViewToken => new FirebaseAuthResult(ViewUid, "view@test.com", EmailVerified: true),
                UpdateToken => new FirebaseAuthResult(UpdateUid, "update@test.com", EmailVerified: true),
                CancelToken => new FirebaseAuthResult(CancelUid, "cancel@test.com", EmailVerified: true),
                NoPermisoToken => new FirebaseAuthResult(NoPermisoUid, "nopermiso@test.com", EmailVerified: true),
                _ => null,
            };

            return Task.FromResult(result);
        }
    }
}
