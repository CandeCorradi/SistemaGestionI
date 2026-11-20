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
using Xunit;

namespace SistemaGestion.API.IntegrationTests;

/// <summary>
/// Tests de API de pagos (api-spec §17): autenticación, autorización
/// granular (PAGOS_VIEW/PAGOS_CREATE), registro con validación de saldo
/// (BR-080/BR-081), saldo calculado por backend (BR-083) e historial
/// inmutable (BR-082).
/// </summary>
public sealed class PaymentsApiTests : IClassFixture<PaymentsApiTests.Factory>
{
    private const string AdminToken = "payments-admin-token";
    private const string ViewToken = "payments-view-token";
    private const string CreateToken = "payments-create-token";
    private const string NoPermisoToken = "payments-nopermiso-token";

    private const string AdminUid = "uid-payments-admin";
    private const string ViewUid = "uid-payments-view";
    private const string CreateUid = "uid-payments-create";
    private const string NoPermisoUid = "uid-payments-nopermiso";

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
                    Permisos.PagosView,
                };
                permisos.EfectivosPorUid[CreateUid] = new List<string>
                {
                    Permisos.PagosView, Permisos.PagosCreate,
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
                usuarios.Seed(ViewUid, "Ver", "Pagos");
                usuarios.Seed(CreateUid, "Crea", "Pagos");
                services.AddSingleton<IUsuarioRepository>(usuarios);

                var pedidos = new FakePedidoRepository(usuarios);
                services.AddSingleton<IPedidoRepository>(pedidos);
                services.AddSingleton<IPresupuestoRepository>(
                    new FakePresupuestoRepository(clientes, pedidos));

                services.AddSingleton<IPagoRepository>(
                    new FakePagoRepository(usuarios, pedidos));
            });
        }
    }

    private readonly Factory _factory;

    private readonly HttpClient _client;

    public PaymentsApiTests(Factory factory)
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

    /// <summary>Crea, envía y acepta un presupuesto vía API para sembrar un pedido (total 250).</summary>
    private async Task<int> SeedAcceptedOrderAsync(string? customerSuffix = null)
    {
        var suffix = customerSuffix ?? Guid.NewGuid().ToString("N");
        var cliente = Clientes.Seed(
            $"pago-{suffix}@test.com", "Orden", "Prueba");

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

    private async Task<HttpResponseMessage> PostPaymentAsync(
        int orderId, object body, string token = AdminToken)
    {
        var request = AuthenticatedRequest(
            HttpMethod.Post, $"/api/v1/orders/{orderId}/payments", token);
        request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }

    private async Task<JsonElement> GetJsonAsync(string url, string token = ViewToken)
    {
        var response = await _client.SendAsync(AuthenticatedRequest(HttpMethod.Get, url, token));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    // ------------------------------------------------------------------
    // Autenticación y autorización
    // ------------------------------------------------------------------

    [Fact]
    public async Task List_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/payments");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_WithoutPagosView_ReturnsForbidden()
    {
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/payments", NoPermisoToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OrderPayments_WithoutPagosView_ReturnsForbidden()
    {
        var request = AuthenticatedRequest(
            HttpMethod.Get, "/api/v1/orders/1/payments", NoPermisoToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithoutPagosCreate_ReturnsForbidden()
    {
        var orderId = await SeedAcceptedOrderAsync();

        var response = await PostPaymentAsync(
            orderId, new { amount = 10m, paymentMethod = 0 }, token: ViewToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ------------------------------------------------------------------
    // Registro (BR-080/BR-081/BR-082)
    // ------------------------------------------------------------------

    [Fact]
    public async Task Register_Valid_ReturnsPaymentWithUserAndUtcDate_Br080()
    {
        var orderId = await SeedAcceptedOrderAsync();

        var response = await PostPaymentAsync(orderId, new
        {
            amount = 100m,
            paymentMethod = 1,
            reference = "TRX-9",
            notes = "Señia inicial",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var pago = body.RootElement;
        Assert.True(pago.GetProperty("id").GetInt32() > 0);
        Assert.Equal(orderId, pago.GetProperty("orderId").GetInt32());
        Assert.Equal(100m, pago.GetProperty("amount").GetDecimal());
        Assert.Equal(1, pago.GetProperty("paymentMethod").GetInt32());
        Assert.Equal("TRX-9", pago.GetProperty("reference").GetString());
        Assert.Equal("Señia inicial", pago.GetProperty("notes").GetString());
        Assert.Equal("Admin Sistema", pago.GetProperty("userName").GetString());
        Assert.Equal(
            DateTimeKind.Utc, pago.GetProperty("date").GetDateTime().Kind);
    }

    [Fact]
    public async Task Register_WithCreatePermission_Ok()
    {
        var orderId = await SeedAcceptedOrderAsync();

        var response = await PostPaymentAsync(
            orderId, new { amount = 10m, paymentMethod = 0 }, token: CreateToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_ZeroAmount_ReturnsBadRequest_Br080()
    {
        var orderId = await SeedAcceptedOrderAsync();

        var response = await PostPaymentAsync(
            orderId, new { amount = 0m, paymentMethod = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ThreeDecimalsAmount_ReturnsBadRequest()
    {
        var orderId = await SeedAcceptedOrderAsync();

        var response = await PostPaymentAsync(
            orderId, new { amount = 10.123m, paymentMethod = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_UndefinedMethod_ReturnsBadRequest()
    {
        var orderId = await SeedAcceptedOrderAsync();

        var response = await PostPaymentAsync(
            orderId, new { amount = 10m, paymentMethod = 99 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_UnknownOrder_ReturnsNotFound()
    {
        var response = await PostPaymentAsync(
            9999, new { amount = 10m, paymentMethod = 0 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Register_Overpay_ReturnsConflict_Br081()
    {
        var orderId = await SeedAcceptedOrderAsync();

        var primero = await PostPaymentAsync(
            orderId, new { amount = 200m, paymentMethod = 0 });
        Assert.Equal(HttpStatusCode.OK, primero.StatusCode);

        var segundo = await PostPaymentAsync(
            orderId, new { amount = 60m, paymentMethod = 0 });

        Assert.Equal(HttpStatusCode.Conflict, segundo.StatusCode);
    }

    [Fact]
    public async Task Register_OnCancelledOrder_ReturnsConflict()
    {
        var orderId = await SeedAcceptedOrderAsync();

        var cancel = AuthenticatedRequest(HttpMethod.Post, $"/api/v1/orders/{orderId}/status");
        cancel.Content = JsonContent.Create(new { status = 5, comment = "Se anuló" });
        var cancelResponse = await _client.SendAsync(cancel);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

        var response = await PostPaymentAsync(
            orderId, new { amount = 10m, paymentMethod = 0 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ------------------------------------------------------------------
    // Saldo por pedido (BR-083)
    // ------------------------------------------------------------------

    [Fact]
    public async Task GetOrderPayments_ReturnsTotalPaidAndBalance_Br083()
    {
        var orderId = await SeedAcceptedOrderAsync();
        await PostPaymentAsync(orderId, new { amount = 100m, paymentMethod = 0 });
        await PostPaymentAsync(orderId, new { amount = 50m, paymentMethod = 1 });

        var body = await GetJsonAsync($"/api/v1/orders/{orderId}/payments");

        Assert.Equal(2, body.GetProperty("items").GetArrayLength());
        Assert.Equal(150m, body.GetProperty("totalPaid").GetDecimal());
        Assert.Equal(100m, body.GetProperty("balance").GetDecimal());
    }

    [Fact]
    public async Task GetOrderPayments_NoPayments_ZeroPaid_FullBalance()
    {
        var orderId = await SeedAcceptedOrderAsync();

        var body = await GetJsonAsync($"/api/v1/orders/{orderId}/payments");

        Assert.Equal(0, body.GetProperty("items").GetArrayLength());
        Assert.Equal(0m, body.GetProperty("totalPaid").GetDecimal());
        Assert.Equal(250m, body.GetProperty("balance").GetDecimal());
    }

    [Fact]
    public async Task GetOrderPayments_UnknownOrder_ReturnsNotFound()
    {
        var response = await _client.SendAsync(
            AuthenticatedRequest(HttpMethod.Get, "/api/v1/orders/9999/payments"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ------------------------------------------------------------------
    // Listado global (AGENTS.md §30)
    // ------------------------------------------------------------------

    [Fact]
    public async Task List_FilterByOrderId_ReturnsOnlyMatch()
    {
        var pedidoA = await SeedAcceptedOrderAsync();
        var pedidoB = await SeedAcceptedOrderAsync();
        await PostPaymentAsync(
            pedidoA, new { amount = 10m, paymentMethod = 0, reference = "PAGO-A" });
        await PostPaymentAsync(
            pedidoB, new { amount = 20m, paymentMethod = 0, reference = "PAGO-B" });

        var body = await GetJsonAsync($"/api/v1/payments?orderId={pedidoA}");

        var items = body.GetProperty("items").EnumerateArray().ToList();
        var item = Assert.Single(items);
        Assert.Equal(pedidoA, item.GetProperty("orderId").GetInt32());
        Assert.Equal("PAGO-A", item.GetProperty("reference").GetString());
        Assert.Equal("Orden Prueba", item.GetProperty("customerName").GetString());
    }

    [Fact]
    public async Task List_SearchOnReference_ReturnsMatch()
    {
        var orderId = await SeedAcceptedOrderAsync();
        await PostPaymentAsync(
            orderId, new { amount = 10m, paymentMethod = 0, reference = "BUSCAR-ESTE" });

        var body = await GetJsonAsync("/api/v1/payments?search=buscar-este");

        var item = Assert.Single(body.GetProperty("items").EnumerateArray());
        Assert.Equal("BUSCAR-ESTE", item.GetProperty("reference").GetString());
    }

    [Fact]
    public async Task List_InvalidOrderId_ReturnsBadRequest()
    {
        var response = await _client.SendAsync(
            AuthenticatedRequest(HttpMethod.Get, "/api/v1/payments?orderId=0"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
                CreateToken => new FirebaseAuthResult(CreateUid, "create@test.com", EmailVerified: true),
                NoPermisoToken => new FirebaseAuthResult(NoPermisoUid, "nopermiso@test.com", EmailVerified: true),
                _ => null,
            };

            return Task.FromResult(result);
        }
    }
}
