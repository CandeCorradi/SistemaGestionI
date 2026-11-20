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
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using Xunit;

namespace SistemaGestion.API.IntegrationTests;

/// <summary>
/// Tests de API de /api/v1/my (Portal Cliente — api-spec §9B): 401 sin
/// token, 404 sin cliente vinculado (BR-010), ownership estricto (Customer
/// Isolation: 404 idéntico para recursos ajenos e inexistentes), solicitud
/// de presupuesto en borrador con ítem sin precio y aceptación atómica
/// con creación de pedido (BR-064/BR-065).
/// </summary>
public sealed class PortalApiTests : IClassFixture<PortalApiTests.Factory>
{
    private const string SinVinculoToken = "portal-sin-vinculo-token";

    private const string SinVinculoUid = "uid-portal-sin-vinculo";

    public sealed class Factory : WebApplicationFactory<Program>
    {
        public TokenStubFirebaseAuthService Stub { get; } = new(SinVinculoToken, SinVinculoUid);

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IFirebaseAuthService>(Stub);

                var permisos = new FakePermisoRepository();
                permisos.EfectivosPorUid[SinVinculoUid] = Permisos.Todos.ToList();
                services.AddSingleton<IPermisoRepository>(permisos);

                var clientes = new FakeClienteRepository();
                services.AddSingleton<IClienteRepository>(clientes);

                var usuarios = new FakeUsuarioRepository();
                usuarios.Seed(SinVinculoUid, "Sin", "Vinculo");
                services.AddSingleton<IUsuarioRepository>(usuarios);

                var pedidos = new FakePedidoRepository(usuarios);
                services.AddSingleton<IPedidoRepository>(pedidos);
                services.AddSingleton<IPresupuestoRepository>(
                    new FakePresupuestoRepository(clientes, pedidos));
                services.AddSingleton<IPagoRepository>(
                    new FakePagoRepository(usuarios, pedidos));
                services.AddSingleton<IProductoRepository>(new FakeProductoRepository());
                services.AddSingleton<INotificacionRepository>(new FakeNotificacionRepository());
            });
        }
    }

    private readonly Factory _factory;

    private readonly HttpClient _client;

    public PortalApiTests(Factory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private FakeClienteRepository Clientes
        => (FakeClienteRepository)_factory.Services.GetRequiredService<IClienteRepository>();

    private FakePresupuestoRepository Presupuestos
        => (FakePresupuestoRepository)_factory.Services.GetRequiredService<IPresupuestoRepository>();

    private FakePedidoRepository Pedidos
        => (FakePedidoRepository)_factory.Services.GetRequiredService<IPedidoRepository>();

    private FakePagoRepository Pagos
        => (FakePagoRepository)_factory.Services.GetRequiredService<IPagoRepository>();

    /// <summary>
    /// Crea un cliente vinculado a un token único (aislamiento entre tests:
    /// cada test usa su propio cliente/token y sólo afirma sobre lo que sembró).
    /// </summary>
    private (string Token, Cliente Cliente) SeedLinkedCliente()
    {
        var uid = $"uid-portal-{Guid.NewGuid():N}";
        var email = $"portal-{Guid.NewGuid():N}@test.com";
        var cliente = Clientes.Seed(email, "Portal", "Cliente", uid);

        var token = $"portal-token-{Guid.NewGuid():N}";
        _factory.Stub.Register(token, uid, email);
        return (token, cliente);
    }

    private HttpRequestMessage AuthenticatedRequest(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private async Task<Presupuesto> SeedQuoteAsync(
        int clienteId,
        EstadoPresupuesto estado = EstadoPresupuesto.Enviado,
        decimal total = 100m)
    {
        var presupuesto = Presupuesto.Crear(
            clienteId, fechaValidezUtc: null, notas: null, condiciones: null,
            fechaCreacionUtc: DateTime.UtcNow);
        presupuesto.AgregarDetalle(PresupuestoDetalle.Crear(
            descripcion: "Mueble a medida", cantidad: 1,
            precioUnitario: total, descuento: 0m));

        if (estado == EstadoPresupuesto.Enviado)
        {
            presupuesto.Enviar(DateTime.UtcNow);
        }

        await Presupuestos.AddAsync(presupuesto);
        return presupuesto;
    }

    private async Task<Pedido> SeedOrderAsync(int clienteId, decimal total = 100m)
    {
        var quote = await SeedQuoteAsync(clienteId, EstadoPresupuesto.Borrador, total);
        var pedido = Pedidos.CrearDesdePresupuesto(quote, DateTime.UtcNow);
        Assert.NotNull(pedido);
        return pedido!;
    }

    private async Task<Pago> SeedPaymentAsync(int pedidoId, decimal monto)
    {
        var pago = Pago.Crear(
            pedidoId, monto, DateTime.UtcNow, MetodoPago.Efectivo,
            referencia: null, notas: null, usuarioId: null);
        await Pagos.AddAsync(pago);
        return pago;
    }

    // ------------------------------------------------------------------
    // Autenticación y vinculación (BR-010)
    // ------------------------------------------------------------------

    [Fact]
    public async Task Profile_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/my/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_WithoutLinkedClient_ReturnsNotFound()
    {
        var request = AuthenticatedRequest(
            HttpMethod.Get, "/api/v1/my/profile", SinVinculoToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("NotFound.Error", problem.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Profile_LinkedClient_ReturnsProfileData()
    {
        var (token, cliente) = SeedLinkedCliente();
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/my/profile", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        Assert.Equal(cliente.Id, root.GetProperty("id").GetInt32());
        Assert.Equal(cliente.Email, root.GetProperty("email").GetString());
        Assert.Equal("Portal", root.GetProperty("nombre").GetString());
        Assert.Equal("Cliente", root.GetProperty("apellido").GetString());
    }

    [Fact]
    public async Task Profile_Update_PersistsContactDataAndKeepsEmail()
    {
        var (token, cliente) = SeedLinkedCliente();
        var request = AuthenticatedRequest(HttpMethod.Patch, "/api/v1/my/profile", token);
        request.Content = JsonContent.Create(new
        {
            nombre = "Nombre Nuevo",
            apellido = (string?)null,
            telefono = "123456789",
            direccion = "Calle Falsa 123",
            documento = (string?)null,
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        Assert.Equal("Nombre Nuevo", root.GetProperty("nombre").GetString());
        Assert.Equal("Cliente", root.GetProperty("apellido").GetString());
        Assert.Equal("123456789", root.GetProperty("telefono").GetString());
        Assert.Equal("Calle Falsa 123", root.GetProperty("direccion").GetString());
        Assert.Equal(cliente.Email, root.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Profile_Update_WithBlankName_ReturnsBadRequest()
    {
        var (token, _) = SeedLinkedCliente();
        var request = AuthenticatedRequest(HttpMethod.Patch, "/api/v1/my/profile", token);
        request.Content = JsonContent.Create(new { nombre = "   " });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Validation.Error", problem.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Profile_Update_WithoutLinkedClient_ReturnsNotFound()
    {
        var request = AuthenticatedRequest(
            HttpMethod.Patch, "/api/v1/my/profile", SinVinculoToken);
        request.Content = JsonContent.Create(new { nombre = "Nombre" });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ------------------------------------------------------------------
    // Solicitud de presupuesto desde el portal
    // ------------------------------------------------------------------

    [Fact]
    public async Task RequestQuote_CreatesDraftWithoutPrice()
    {
        var (token, cliente) = SeedLinkedCliente();
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/my/quotes", token);
        request.Content = JsonContent.Create(new
        {
            description = "Mueble a medida 2x1",
            cantidad = 2,
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        var quoteId = root.GetProperty("id").GetInt32();
        Assert.Equal(
            $"/api/v1/my/quotes/{quoteId}",
            response.Headers.Location?.ToString());
        Assert.Equal(cliente.Id, root.GetProperty("customerId").GetInt32());
        Assert.Equal((int)EstadoPresupuesto.Borrador, root.GetProperty("status").GetInt32());
        Assert.Equal(0m, root.GetProperty("total").GetDecimal());

        var item = root.GetProperty("items").EnumerateArray().Single();
        Assert.Equal(JsonValueKind.Null, item.GetProperty("productId").ValueKind);
        Assert.Equal("Mueble a medida 2x1", item.GetProperty("description").GetString());
        Assert.Equal(2, item.GetProperty("quantity").GetInt32());
        Assert.Equal(0m, item.GetProperty("unitPrice").GetDecimal());

        Assert.Contains(
            Presupuestos.All,
            p => p.Id == quoteId && p.Estado == EstadoPresupuesto.Borrador);
    }

    [Fact]
    public async Task RequestQuote_WithBlankDescription_ReturnsBadRequest()
    {
        var (token, _) = SeedLinkedCliente();
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/my/quotes", token);
        request.Content = JsonContent.Create(new { description = "   ", cantidad = 1 });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Validation.Error", problem.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task RequestQuote_WithoutLinkedClient_ReturnsNotFound()
    {
        var request = AuthenticatedRequest(
            HttpMethod.Post, "/api/v1/my/quotes", SinVinculoToken);
        request.Content = JsonContent.Create(new { description = "Mueble", cantidad = 1 });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Quotes_List_ReturnsOnlyOwnQuotes()
    {
        var (token, cliente) = SeedLinkedCliente();
        var (_, ajeno) = SeedLinkedCliente();
        var propia = await SeedQuoteAsync(cliente.Id, total: 150m);
        await SeedQuoteAsync(ajeno.Id, total: 250m);

        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/my/quotes", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = body.RootElement.GetProperty("items").EnumerateArray().ToList();
        var item = Assert.Single(items);
        Assert.Equal(cliente.Id, item.GetProperty("customerId").GetInt32());
        Assert.Equal(propia.Id, item.GetProperty("id").GetInt32());
        Assert.Equal(150m, item.GetProperty("total").GetDecimal());
        Assert.Equal(1, body.RootElement.GetProperty("totalItems").GetInt32());
    }

    [Fact]
    public async Task Quotes_Get_ForeignQuote_ReturnsNotFound()
    {
        var (token, _) = SeedLinkedCliente();
        var (_, ajeno) = SeedLinkedCliente();
        var ajena = await SeedQuoteAsync(ajeno.Id);

        var request = AuthenticatedRequest(
            HttpMethod.Get, $"/api/v1/my/quotes/{ajena.Id}", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Quotes_Get_UnknownQuote_ReturnsNotFound()
    {
        var (token, _) = SeedLinkedCliente();

        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/my/quotes/999999", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ------------------------------------------------------------------
    // Aceptación atómica (BR-064/BR-065)
    // ------------------------------------------------------------------

    [Fact]
    public async Task AcceptQuote_OwnSentQuote_CreatesOrder()
    {
        var (token, cliente) = SeedLinkedCliente();
        var quote = await SeedQuoteAsync(cliente.Id, total: 300m);

        var request = AuthenticatedRequest(
            HttpMethod.Post, $"/api/v1/my/quotes/{quote.Id}/accept", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        Assert.Equal((int)EstadoPresupuesto.Aceptado, root.GetProperty("status").GetInt32());
        var orderId = root.GetProperty("orderId").GetInt32();
        Assert.True(orderId > 0);

        var pedido = Assert.Single(Pedidos.Pedidos, p => p.ClienteId == cliente.Id);
        Assert.Equal(orderId, pedido.Id);
        Assert.Equal(300m, pedido.Total);
        Assert.Equal(EstadoPresupuesto.Aceptado, quote.Estado);
    }

    [Fact]
    public async Task AcceptQuote_ForeignQuote_ReturnsNotFoundAndCreatesNoOrder()
    {
        var (token, _) = SeedLinkedCliente();
        var (_, ajeno) = SeedLinkedCliente();
        var ajena = await SeedQuoteAsync(ajeno.Id);

        var request = AuthenticatedRequest(
            HttpMethod.Post, $"/api/v1/my/quotes/{ajena.Id}/accept", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain(Pedidos.Pedidos, p => p.PresupuestoId == ajena.Id);
        Assert.Equal(EstadoPresupuesto.Enviado, ajena.Estado);
    }

    [Fact]
    public async Task AcceptQuote_DraftQuote_ReturnsConflict()
    {
        var (token, cliente) = SeedLinkedCliente();
        var quote = await SeedQuoteAsync(cliente.Id, EstadoPresupuesto.Borrador);

        var request = AuthenticatedRequest(
            HttpMethod.Post, $"/api/v1/my/quotes/{quote.Id}/accept", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Conflict.Error", problem.RootElement.GetProperty("title").GetString());
        Assert.DoesNotContain(Pedidos.Pedidos, p => p.PresupuestoId == quote.Id);
    }

    // ------------------------------------------------------------------
    // Pedidos (Customer Isolation)
    // ------------------------------------------------------------------

    [Fact]
    public async Task Orders_List_ReturnsOnlyOwnOrders()
    {
        var (token, cliente) = SeedLinkedCliente();
        var (_, ajeno) = SeedLinkedCliente();
        var propia = await SeedOrderAsync(cliente.Id, total: 120m);
        await SeedOrderAsync(ajeno.Id, total: 999m);

        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/my/orders", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var item = Assert.Single(body.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(cliente.Id, item.GetProperty("customerId").GetInt32());
        Assert.Equal(propia.Id, item.GetProperty("id").GetInt32());
        Assert.Equal(120m, item.GetProperty("total").GetDecimal());
    }

    [Fact]
    public async Task Orders_Get_ForeignOrder_ReturnsNotFound()
    {
        var (token, _) = SeedLinkedCliente();
        var (_, ajeno) = SeedLinkedCliente();
        var ajenoPedido = await SeedOrderAsync(ajeno.Id);

        var request = AuthenticatedRequest(
            HttpMethod.Get, $"/api/v1/my/orders/{ajenoPedido.Id}", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Orders_History_OwnOrder_ReturnsEntries()
    {
        var (token, cliente) = SeedLinkedCliente();
        var pedido = await SeedOrderAsync(cliente.Id);

        var request = AuthenticatedRequest(
            HttpMethod.Get, $"/api/v1/my/orders/{pedido.Id}/history", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var entry = Assert.Single(body.RootElement.EnumerateArray());
        Assert.Equal((int)EstadoPedido.Pendiente, entry.GetProperty("newStatus").GetInt32());
        Assert.Equal(JsonValueKind.Null, entry.GetProperty("previousStatus").ValueKind);
    }

    [Fact]
    public async Task Orders_History_ForeignOrder_ReturnsNotFound()
    {
        var (token, _) = SeedLinkedCliente();
        var (_, ajeno) = SeedLinkedCliente();
        var ajenoPedido = await SeedOrderAsync(ajeno.Id);

        var request = AuthenticatedRequest(
            HttpMethod.Get, $"/api/v1/my/orders/{ajenoPedido.Id}/history", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Orders_Payments_OwnOrder_ReturnsBalance()
    {
        var (token, cliente) = SeedLinkedCliente();
        var pedido = await SeedOrderAsync(cliente.Id, total: 300m);
        await SeedPaymentAsync(pedido.Id, 100m);

        var request = AuthenticatedRequest(
            HttpMethod.Get, $"/api/v1/my/orders/{pedido.Id}/payments", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        Assert.Equal(100m, root.GetProperty("totalPaid").GetDecimal());
        Assert.Equal(200m, root.GetProperty("balance").GetDecimal());
        var item = Assert.Single(root.GetProperty("items").EnumerateArray());
        Assert.Equal(100m, item.GetProperty("amount").GetDecimal());
        Assert.Equal(pedido.Id, item.GetProperty("orderId").GetInt32());
    }

    [Fact]
    public async Task Orders_Payments_ForeignOrder_ReturnsNotFound()
    {
        var (token, _) = SeedLinkedCliente();
        var (_, ajeno) = SeedLinkedCliente();
        var ajenoPedido = await SeedOrderAsync(ajeno.Id);

        var request = AuthenticatedRequest(
            HttpMethod.Get, $"/api/v1/my/orders/{ajenoPedido.Id}/payments", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ------------------------------------------------------------------
    // Pagos y resumen (Customer Isolation)
    // ------------------------------------------------------------------

    [Fact]
    public async Task Payments_List_ReturnsOnlyOwnPayments()
    {
        var (token, cliente) = SeedLinkedCliente();
        var (_, ajeno) = SeedLinkedCliente();
        var pedidoPropio = await SeedOrderAsync(cliente.Id, total: 300m);
        var pedidoAjeno = await SeedOrderAsync(ajeno.Id, total: 300m);
        await SeedPaymentAsync(pedidoPropio.Id, 50m);
        await SeedPaymentAsync(pedidoAjeno.Id, 75m);

        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/my/payments", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var item = Assert.Single(body.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(50m, item.GetProperty("amount").GetDecimal());
        Assert.Equal(pedidoPropio.Id, item.GetProperty("orderId").GetInt32());
    }

    [Fact]
    public async Task Summary_ReturnsBalancesAndPendingCounters()
    {
        var (token, cliente) = SeedLinkedCliente();
        var pedido = await SeedOrderAsync(cliente.Id, total: 300m);
        await SeedPaymentAsync(pedido.Id, 100m);
        await SeedQuoteAsync(cliente.Id, EstadoPresupuesto.Enviado, total: 50m);
        await SeedQuoteAsync(cliente.Id, EstadoPresupuesto.Borrador, total: 60m);

        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/my/summary", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        Assert.Equal(300m, root.GetProperty("totalPedidos").GetDecimal());
        Assert.Equal(100m, root.GetProperty("totalPagado").GetDecimal());
        Assert.Equal(200m, root.GetProperty("saldo").GetDecimal());
        Assert.Equal(1, root.GetProperty("pedidosCount").GetInt32());
        Assert.Equal(1, root.GetProperty("presupuestosPendientes").GetInt32());
    }

    [Fact]
    public async Task Summary_WithoutLinkedClient_ReturnsNotFound()
    {
        var request = AuthenticatedRequest(
            HttpMethod.Get, "/api/v1/my/summary", SinVinculoToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ------------------------------------------------------------------
    // Stub de autenticación: tokens fijos + tokens únicos por test
    // ------------------------------------------------------------------

    public sealed class TokenStubFirebaseAuthService : IFirebaseAuthService
    {
        private readonly Dictionary<string, FirebaseAuthResult> _tokens = new();

        public TokenStubFirebaseAuthService(string sinVinculoToken, string sinVinculoUid)
        {
            _tokens[sinVinculoToken] = new FirebaseAuthResult(
                sinVinculoUid, "sin-vinculo@test.com", EmailVerified: true);
        }

        /// <summary>Registra un token único para un test (aislamiento).</summary>
        public void Register(string token, string uid, string email)
            => _tokens[token] = new FirebaseAuthResult(uid, email, EmailVerified: true);

        public Task<FirebaseAuthResult?> VerifyTokenAsync(
            string idToken,
            CancellationToken cancellationToken = default)
        {
            FirebaseAuthResult? result = _tokens.TryGetValue(idToken, out var found)
                ? found
                : null;

            return Task.FromResult(result);
        }
    }
}
