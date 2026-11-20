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
/// Tests de API de facturación simulada (api-spec §17A): autenticación,
/// autorización granular (FACTURAS_VIEW/FACTURAS_CREATE), emisión sólo
/// para pedidos entregados o completados, una factura por pedido (409),
/// snapshot histórico (BR-141) y numeración simulada SIM-###### (BR-130).
/// </summary>
public sealed class InvoicesApiTests : IClassFixture<InvoicesApiTests.Factory>
{
    private const string AdminToken = "invoices-admin-token";
    private const string ViewToken = "invoices-view-token";
    private const string CreateToken = "invoices-create-token";
    private const string NoPermisoToken = "invoices-nopermiso-token";

    private const string AdminUid = "uid-invoices-admin";
    private const string ViewUid = "uid-invoices-view";
    private const string CreateUid = "uid-invoices-create";
    private const string NoPermisoUid = "uid-invoices-nopermiso";

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
                    Permisos.FacturasView,
                };
                permisos.EfectivosPorUid[CreateUid] = new List<string>
                {
                    Permisos.FacturasView, Permisos.FacturasCreate,
                };
                permisos.EfectivosPorUid[NoPermisoUid] = new List<string>
                {
                    Permisos.PagosView,
                };
                services.AddSingleton<IPermisoRepository>(permisos);

                var clientes = new FakeClienteRepository();
                services.AddSingleton<IClienteRepository>(clientes);

                var productos = new FakeProductoRepository();
                services.AddSingleton<IProductoRepository>(productos);

                var usuarios = new FakeUsuarioRepository();
                usuarios.Seed(AdminUid, "Admin", "Sistema");
                usuarios.Seed(ViewUid, "Ver", "Facturas");
                usuarios.Seed(CreateUid, "Crea", "Facturas");
                services.AddSingleton<IUsuarioRepository>(usuarios);

                var pedidos = new FakePedidoRepository(usuarios);
                services.AddSingleton<IPedidoRepository>(pedidos);
                services.AddSingleton<IPresupuestoRepository>(
                    new FakePresupuestoRepository(clientes, pedidos));

                services.AddSingleton<IFacturaRepository>(
                    new FakeFacturaRepository(usuarios, pedidos));
            });
        }
    }

    private readonly Factory _factory;

    private readonly HttpClient _client;

    public InvoicesApiTests(Factory factory)
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
            $"factura-{suffix}@test.com", "Orden", "Prueba");

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

    /// <summary>Avanza el pedido por el ciclo hasta Entregado (estado 4).</summary>
    private async Task<int> SeedDeliveredOrderAsync()
    {
        var orderId = await SeedAcceptedOrderAsync();

        foreach (var status in new[] { 1, 2, 3, 4 })
        {
            var request = AuthenticatedRequest(
                HttpMethod.Post, $"/api/v1/orders/{orderId}/status");
            request.Content = JsonContent.Create(new { status, comment = "Avance de producción" });
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        return orderId;
    }

    private async Task<HttpResponseMessage> PostInvoiceAsync(
        int orderId, string? notes = null, string token = AdminToken)
    {
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/facturas", token);
        request.Content = JsonContent.Create(new { orderId, notes });
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
        var response = await _client.GetAsync("/api/v1/facturas");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_WithoutFacturasView_ReturnsForbidden()
    {
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/facturas", NoPermisoToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithoutFacturasView_ReturnsForbidden()
    {
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/facturas/1", NoPermisoToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutFacturasCreate_ReturnsForbidden()
    {
        var orderId = await SeedAcceptedOrderAsync();

        var response = await PostInvoiceAsync(orderId, token: ViewToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ------------------------------------------------------------------
    // Emisión (BR-130/BR-141)
    // ------------------------------------------------------------------

    [Fact]
    public async Task Create_DeliveredOrder_ReturnsSimulatedInvoiceWithSnapshot_Br130()
    {
        var orderId = await SeedDeliveredOrderAsync();

        var response = await PostInvoiceAsync(orderId, notes: "Factura de entrega");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var factura = body.RootElement;
        Assert.True(factura.GetProperty("id").GetInt32() > 0);
        Assert.Equal(orderId, factura.GetProperty("orderId").GetInt32());
        Assert.Equal(250m, factura.GetProperty("total").GetDecimal());
        Assert.Equal("Orden Prueba", factura.GetProperty("customerName").GetString());
        Assert.Equal("Admin Sistema", factura.GetProperty("userName").GetString());
        Assert.Equal("Factura de entrega", factura.GetProperty("notes").GetString());
        Assert.Equal(0, factura.GetProperty("status").GetInt32());
        Assert.True(factura.GetProperty("isSimulated").GetBoolean());
        Assert.StartsWith("SIM-", factura.GetProperty("number").GetString());
        Assert.Equal(
            DateTimeKind.Utc, factura.GetProperty("issueDate").GetDateTime().Kind);
    }

    [Fact]
    public async Task Create_WithCreatePermission_Ok()
    {
        var orderId = await SeedDeliveredOrderAsync();

        var response = await PostInvoiceAsync(orderId, token: CreateToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Create_PendingOrder_ReturnsConflict()
    {
        var orderId = await SeedAcceptedOrderAsync();

        var response = await PostInvoiceAsync(orderId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_UnknownOrder_ReturnsNotFound()
    {
        var response = await PostInvoiceAsync(9999);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidOrderId_ReturnsBadRequest()
    {
        var response = await PostInvoiceAsync(0);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateInvoice_ReturnsConflict()
    {
        var orderId = await SeedDeliveredOrderAsync();

        var primero = await PostInvoiceAsync(orderId);
        Assert.Equal(HttpStatusCode.OK, primero.StatusCode);

        var segundo = await PostInvoiceAsync(orderId);

        Assert.Equal(HttpStatusCode.Conflict, segundo.StatusCode);
    }

    // ------------------------------------------------------------------
    // Detalle
    // ------------------------------------------------------------------

    [Fact]
    public async Task GetById_Existing_ReturnsInvoice()
    {
        var orderId = await SeedDeliveredOrderAsync();
        using var creado = JsonDocument.Parse(
            await (await PostInvoiceAsync(orderId)).Content.ReadAsStringAsync());
        var id = creado.RootElement.GetProperty("id").GetInt32();

        var body = await GetJsonAsync($"/api/v1/facturas/{id}");

        Assert.Equal(id, body.GetProperty("id").GetInt32());
        Assert.Equal(orderId, body.GetProperty("orderId").GetInt32());
        Assert.StartsWith("SIM-", body.GetProperty("number").GetString());
        Assert.Equal("Admin Sistema", body.GetProperty("userName").GetString());
    }

    [Fact]
    public async Task GetById_Unknown_ReturnsNotFound()
    {
        var response = await _client.SendAsync(
            AuthenticatedRequest(HttpMethod.Get, "/api/v1/facturas/9999"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_InvalidId_ReturnsBadRequest()
    {
        var response = await _client.SendAsync(
            AuthenticatedRequest(HttpMethod.Get, "/api/v1/facturas/0"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ------------------------------------------------------------------
    // Listado global (AGENTS.md §30)
    // ------------------------------------------------------------------

    [Fact]
    public async Task List_FilterByOrderId_ReturnsOnlyMatch()
    {
        var pedidoA = await SeedDeliveredOrderAsync();
        var pedidoB = await SeedDeliveredOrderAsync();
        await PostInvoiceAsync(pedidoA);
        await PostInvoiceAsync(pedidoB);

        var body = await GetJsonAsync($"/api/v1/facturas?orderId={pedidoA}");

        var item = Assert.Single(body.GetProperty("items").EnumerateArray());
        Assert.Equal(pedidoA, item.GetProperty("orderId").GetInt32());
        Assert.Equal("Orden Prueba", item.GetProperty("customerName").GetString());
    }

    [Fact]
    public async Task List_SearchOnNumber_ReturnsMatch()
    {
        var orderId = await SeedDeliveredOrderAsync();
        using var creado = JsonDocument.Parse(
            await (await PostInvoiceAsync(orderId)).Content.ReadAsStringAsync());
        var numero = creado.RootElement.GetProperty("number").GetString();

        var body = await GetJsonAsync(
            $"/api/v1/facturas?search={Uri.EscapeDataString(numero!)}");

        var item = Assert.Single(body.GetProperty("items").EnumerateArray());
        Assert.Equal(numero, item.GetProperty("number").GetString());
    }

    [Fact]
    public async Task List_FilterByCustomerId_ReturnsOnlyMatch()
    {
        var pedido = await SeedDeliveredOrderAsync();
        using var creado = JsonDocument.Parse(
            await (await PostInvoiceAsync(pedido)).Content.ReadAsStringAsync());
        var customerId = creado.RootElement.GetProperty("customerId").GetInt32();

        var body = await GetJsonAsync($"/api/v1/facturas?customerId={customerId}");

        Assert.All(
            body.GetProperty("items").EnumerateArray(),
            item => Assert.Equal(customerId, item.GetProperty("customerId").GetInt32()));
    }

    [Fact]
    public async Task List_InvalidOrderId_ReturnsBadRequest()
    {
        var response = await _client.SendAsync(
            AuthenticatedRequest(HttpMethod.Get, "/api/v1/facturas?orderId=0"));

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
