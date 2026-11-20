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
/// Tests de API de /api/v1/quotes (api-spec §15): autenticación,
/// autorización granular (VIEW/CREATE/UPDATE/ANULATE), snapshot de precios
/// (BR-063), totales en backend (BR-062) y transiciones (BR-064).
/// </summary>
public sealed class QuotesApiTests : IClassFixture<QuotesApiTests.Factory>
{
    private const string AdminToken = "quotes-admin-token";
    private const string ViewToken = "quotes-view-token";
    private const string CreateToken = "quotes-create-token";
    private const string UpdateToken = "quotes-update-token";
    private const string AnulateToken = "quotes-anulate-token";
    private const string AcceptToken = "quotes-accept-token";
    private const string NoPermisoToken = "quotes-nopermiso-token";

    private const string AdminUid = "uid-quotes-admin";
    private const string ViewUid = "uid-quotes-view";
    private const string CreateUid = "uid-quotes-create";
    private const string UpdateUid = "uid-quotes-update";
    private const string AnulateUid = "uid-quotes-anulate";
    private const string AcceptUid = "uid-quotes-accept";
    private const string NoPermisoUid = "uid-quotes-nopermiso";

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
                    Permisos.PresupuestosView,
                };
                permisos.EfectivosPorUid[CreateUid] = new List<string>
                {
                    Permisos.PresupuestosView, Permisos.PresupuestosCreate,
                };
                permisos.EfectivosPorUid[UpdateUid] = new List<string>
                {
                    Permisos.PresupuestosView, Permisos.PresupuestosUpdate,
                };
                permisos.EfectivosPorUid[AnulateUid] = new List<string>
                {
                    Permisos.PresupuestosView, Permisos.PresupuestosAnulate,
                };
                permisos.EfectivosPorUid[AcceptUid] = new List<string>
                {
                    Permisos.PresupuestosAccept,
                };
                permisos.EfectivosPorUid[NoPermisoUid] = new List<string>
                {
                    Permisos.UsuariosView,
                };
                services.AddSingleton<IPermisoRepository>(permisos);

                var clientes = new FakeClienteRepository();
                services.AddSingleton<IClienteRepository>(clientes);
                services.AddSingleton<IPresupuestoRepository>(new FakePresupuestoRepository(clientes));

                var productos = new FakeProductoRepository();
                services.AddSingleton<IProductoRepository>(productos);

                var usuarios = new FakeUsuarioRepository();
                usuarios.Seed(AdminUid, "Admin", "Sistema");
                usuarios.Seed(CreateUid, "Crea", "Usuario");
                services.AddSingleton<IUsuarioRepository>(usuarios);
            });
        }
    }

    private readonly Factory _factory;

    private readonly HttpClient _client;

    public QuotesApiTests(Factory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private FakeClienteRepository Clientes
        => (FakeClienteRepository)_factory.Services.GetRequiredService<IClienteRepository>();

    private FakeProductoRepository Productos
        => (FakeProductoRepository)_factory.Services.GetRequiredService<IProductoRepository>();

    private FakePresupuestoRepository Presupuestos
        => (FakePresupuestoRepository)_factory.Services.GetRequiredService<IPresupuestoRepository>();

    private HttpRequestMessage AuthenticatedRequest(HttpMethod method, string url, string token = AdminToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static object NewQuoteBody(int customerId, params object[] items)
    {
        // Sin líneas explícitas se crea una por defecto (BR-061 exige ítems).
        if (items.Length == 0)
        {
            items = new[]
            {
                new { productId = (int?)null, description = "Mueble a medida", quantity = 1, unitPrice = 100m, discount = (decimal?)null },
            };
        }

        return new
        {
            customerId,
            expirationDate = (string?)null,
            notes = "Notas de prueba",
            conditions = "50% de seña",
            items,
        };
    }

    private async Task<JsonDocument> CreateQuoteAsync(int customerId, string token = AdminToken, params object[] items)
    {
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/quotes", token);
        request.Content = JsonContent.Create(NewQuoteBody(customerId, items));

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task List_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/quotes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_WithoutPresupuestosView_ReturnsForbidden()
    {
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/quotes", NoPermisoToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task List_WithView_ReturnsQuotesForCustomer()
    {
        var cliente = Clientes.Seed("list-quotes@test.com", "Lista", "Cliente");
        await CreateQuoteAsync(cliente.Id, AdminToken,
            new { productId = (int?)null, description = "Mueble", quantity = 1, unitPrice = 100m, discount = (decimal?)null });

        var request = AuthenticatedRequest(
            HttpMethod.Get, $"/api/v1/quotes?customerId={cliente.Id}", ViewToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var item = Assert.Single(body.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(cliente.Id, item.GetProperty("customerId").GetInt32());
        Assert.Equal("Lista Cliente", item.GetProperty("customerName").GetString());
        Assert.Equal(100m, item.GetProperty("total").GetDecimal());
        Assert.Equal(0, item.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Create_WithProductLine_SnapshotsCurrentPrice_Br063()
    {
        var cliente = Clientes.Seed("snapshot@test.com", "Snap", "Shot");
        var producto = Productos.Seed(nombre: "Melamina Blanca", precio: 1500m);

        using var quote = await CreateQuoteAsync(cliente.Id, AdminToken,
            new { productId = producto.Id, description = (string?)null, quantity = 2, unitPrice = (decimal?)null, discount = (decimal?)null });

        var item = Assert.Single(quote.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(1500m, item.GetProperty("unitPrice").GetDecimal());
        Assert.Equal("Melamina Blanca", item.GetProperty("description").GetString());
        Assert.Equal(3000m, quote.RootElement.GetProperty("total").GetDecimal());
    }

    [Fact]
    public async Task Create_WithoutCreatePermission_ReturnsForbidden()
    {
        var cliente = Clientes.Seed("forbidden-create@test.com");
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/quotes", ViewToken);
        request.Content = JsonContent.Create(NewQuoteBody(cliente.Id,
            new { productId = (int?)null, description = "Mueble", quantity = 1, unitPrice = 100m, discount = (decimal?)null }));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutItems_ReturnsBadRequest_Br061()
    {
        var cliente = Clientes.Seed("no-items@test.com");
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/quotes");
        request.Content = JsonContent.Create(new
        {
            customerId = cliente.Id,
            expirationDate = (string?)null,
            notes = (string?)null,
            conditions = (string?)null,
            items = Array.Empty<object>(),
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains("BR-061", body.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Create_UnknownCustomer_ReturnsNotFound()
    {
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/quotes");
        request.Content = JsonContent.Create(NewQuoteBody(9999,
            new { productId = (int?)null, description = "Mueble", quantity = 1, unitPrice = 100m, discount = (decimal?)null }));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Existing_ReturnsDetail()
    {
        var cliente = Clientes.Seed("detail-quote@test.com", "Detalle", "Cliente");
        using var creado = await CreateQuoteAsync(cliente.Id);
        var id = creado.RootElement.GetProperty("id").GetInt32();

        var request = AuthenticatedRequest(HttpMethod.Get, $"/api/v1/quotes/{id}", ViewToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(id, body.RootElement.GetProperty("id").GetInt32());
        Assert.Single(body.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal("Notas de prueba", body.RootElement.GetProperty("notes").GetString());
    }

    [Fact]
    public async Task GetById_Unknown_ReturnsNotFound()
    {
        var request = AuthenticatedRequest(HttpMethod.Get, "/api/v1/quotes/9999", ViewToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Patch_InBorrador_ReturnsOk_AndRecalculatesTotals()
    {
        var cliente = Clientes.Seed("patch-quote@test.com");
        using var creado = await CreateQuoteAsync(cliente.Id);
        var id = creado.RootElement.GetProperty("id").GetInt32();

        var request = AuthenticatedRequest(HttpMethod.Patch, $"/api/v1/quotes/{id}", UpdateToken);
        request.Content = JsonContent.Create(new
        {
            customerId = cliente.Id,
            expirationDate = (string?)null,
            notes = "Editado",
            conditions = (string?)null,
            items = new[]
            {
                new { productId = (int?)null, description = "Línea nueva", quantity = 4, unitPrice = 50m, discount = 10m },
            },
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Editado", body.RootElement.GetProperty("notes").GetString());
        Assert.Equal(200m, body.RootElement.GetProperty("subtotal").GetDecimal());
        Assert.Equal(10m, body.RootElement.GetProperty("discount").GetDecimal());
        Assert.Equal(190m, body.RootElement.GetProperty("total").GetDecimal());
    }

    [Fact]
    public async Task Patch_Enviado_ReturnsConflict()
    {
        var cliente = Clientes.Seed("patch-sent@test.com");
        using var creado = await CreateQuoteAsync(cliente.Id);
        var id = creado.RootElement.GetProperty("id").GetInt32();
        await _client.SendAsync(AuthenticatedRequest(HttpMethod.Post, $"/api/v1/quotes/{id}/send"));

        var request = AuthenticatedRequest(HttpMethod.Patch, $"/api/v1/quotes/{id}", UpdateToken);
        request.Content = JsonContent.Create(new
        {
            customerId = cliente.Id,
            expirationDate = (string?)null,
            notes = (string?)null,
            conditions = (string?)null,
            items = (object?)null,
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Send_FromBorrador_Ok_ThenSecondSend_ReturnsConflict()
    {
        var cliente = Clientes.Seed("send-quote@test.com");
        using var creado = await CreateQuoteAsync(cliente.Id);
        var id = creado.RootElement.GetProperty("id").GetInt32();

        var primero = await _client.SendAsync(
            AuthenticatedRequest(HttpMethod.Post, $"/api/v1/quotes/{id}/send", UpdateToken));
        Assert.Equal(HttpStatusCode.OK, primero.StatusCode);

        var segundo = await _client.SendAsync(
            AuthenticatedRequest(HttpMethod.Post, $"/api/v1/quotes/{id}/send", UpdateToken));
        Assert.Equal(HttpStatusCode.Conflict, segundo.StatusCode);
    }

    [Fact]
    public async Task Send_WithoutUpdatePermission_ReturnsForbidden()
    {
        var cliente = Clientes.Seed("send-forbidden@test.com");
        using var creado = await CreateQuoteAsync(cliente.Id);
        var id = creado.RootElement.GetProperty("id").GetInt32();

        var response = await _client.SendAsync(
            AuthenticatedRequest(HttpMethod.Post, $"/api/v1/quotes/{id}/send", CreateToken));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Reject_FromEnviado_Ok_StoresReason()
    {
        var cliente = Clientes.Seed("reject-quote@test.com");
        using var creado = await CreateQuoteAsync(cliente.Id);
        var id = creado.RootElement.GetProperty("id").GetInt32();
        await _client.SendAsync(AuthenticatedRequest(HttpMethod.Post, $"/api/v1/quotes/{id}/send"));

        var request = AuthenticatedRequest(HttpMethod.Post, $"/api/v1/quotes/{id}/reject", UpdateToken);
        request.Content = JsonContent.Create(new { reason = "Muy caro" });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(3, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Muy caro", body.RootElement.GetProperty("stateReason").GetString());
    }

    [Fact]
    public async Task Cancel_FromBorrador_Ok()
    {
        var cliente = Clientes.Seed("cancel-quote@test.com");
        using var creado = await CreateQuoteAsync(cliente.Id);
        var id = creado.RootElement.GetProperty("id").GetInt32();

        var request = AuthenticatedRequest(HttpMethod.Post, $"/api/v1/quotes/{id}/cancel", AnulateToken);
        request.Content = JsonContent.Create(new { reason = "Se anuló" });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(4, body.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Cancel_WithoutAnulatePermission_ReturnsForbidden()
    {
        var cliente = Clientes.Seed("cancel-forbidden@test.com");
        using var creado = await CreateQuoteAsync(cliente.Id);
        var id = creado.RootElement.GetProperty("id").GetInt32();

        var request = AuthenticatedRequest(HttpMethod.Post, $"/api/v1/quotes/{id}/cancel", UpdateToken);
        request.Content = JsonContent.Create(new { reason = (string?)null });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task List_InvalidDateRange_ReturnsBadRequest()
    {
        var request = AuthenticatedRequest(
            HttpMethod.Get, "/api/v1/quotes?dateFrom=2026-10-10&dateTo=2026-10-01", AdminToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_PastExpirationDate_ReturnsBadRequest()
    {
        var cliente = Clientes.Seed("past-expiration@test.com");
        var request = AuthenticatedRequest(HttpMethod.Post, "/api/v1/quotes");
        request.Content = JsonContent.Create(new
        {
            customerId = cliente.Id,
            expirationDate = "2020-01-01",
            notes = (string?)null,
            conditions = (string?)null,
            items = new[]
            {
                new { productId = (int?)null, description = "Mueble", quantity = 1, unitPrice = 100m, discount = (decimal?)null },
            },
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Accept_FromEnviado_ReturnsOk_CreatesOrder_Br065()
    {
        var cliente = Clientes.Seed("accept-quote@test.com");
        using var creado = await CreateQuoteAsync(cliente.Id);
        var id = creado.RootElement.GetProperty("id").GetInt32();
        var total = creado.RootElement.GetProperty("total").GetDecimal();
        await _client.SendAsync(AuthenticatedRequest(HttpMethod.Post, $"/api/v1/quotes/{id}/send"));

        var response = await _client.SendAsync(
            AuthenticatedRequest(HttpMethod.Post, $"/api/v1/quotes/{id}/accept", AcceptToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(2, body.RootElement.GetProperty("status").GetInt32());
        var orderId = body.RootElement.GetProperty("orderId").GetInt32();
        Assert.True(orderId > 0);

        var pedido = Assert.Single(Presupuestos.Pedidos, p => p.PresupuestoId == id);
        Assert.Equal(orderId, pedido.Id);
        Assert.Equal(total, pedido.Total);
        Assert.Equal(EstadoPedido.Pendiente, pedido.Estado);

        var detalle = AuthenticatedRequest(HttpMethod.Get, $"/api/v1/quotes/{id}", ViewToken);
        using var detailBody = JsonDocument.Parse(
            await (await _client.SendAsync(detalle)).Content.ReadAsStringAsync());
        Assert.Equal(orderId, detailBody.RootElement.GetProperty("orderId").GetInt32());
    }

    [Fact]
    public async Task Accept_WithoutAcceptPermission_ReturnsForbidden()
    {
        var cliente = Clientes.Seed("accept-forbidden@test.com");
        using var creado = await CreateQuoteAsync(cliente.Id);
        var id = creado.RootElement.GetProperty("id").GetInt32();
        await _client.SendAsync(AuthenticatedRequest(HttpMethod.Post, $"/api/v1/quotes/{id}/send"));

        var response = await _client.SendAsync(
            AuthenticatedRequest(HttpMethod.Post, $"/api/v1/quotes/{id}/accept", ViewToken));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Accept_SecondTime_ReturnsConflict_NoSecondOrder()
    {
        var cliente = Clientes.Seed("accept-twice@test.com");
        using var creado = await CreateQuoteAsync(cliente.Id);
        var id = creado.RootElement.GetProperty("id").GetInt32();
        await _client.SendAsync(AuthenticatedRequest(HttpMethod.Post, $"/api/v1/quotes/{id}/send"));
        await _client.SendAsync(AuthenticatedRequest(HttpMethod.Post, $"/api/v1/quotes/{id}/accept"));

        var response = await _client.SendAsync(
            AuthenticatedRequest(HttpMethod.Post, $"/api/v1/quotes/{id}/accept"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Single(Presupuestos.Pedidos, p => p.PresupuestoId == id);
    }

    [Fact]
    public async Task Accept_FromBorrador_ReturnsConflict()
    {
        var cliente = Clientes.Seed("accept-borrador@test.com");
        using var creado = await CreateQuoteAsync(cliente.Id);
        var id = creado.RootElement.GetProperty("id").GetInt32();

        var response = await _client.SendAsync(
            AuthenticatedRequest(HttpMethod.Post, $"/api/v1/quotes/{id}/accept"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.DoesNotContain(Presupuestos.Pedidos, p => p.PresupuestoId == id);
    }

    [Fact]
    public async Task Accept_UnknownQuote_ReturnsNotFound()
    {
        var response = await _client.SendAsync(
            AuthenticatedRequest(HttpMethod.Post, "/api/v1/quotes/9999/accept"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void EstadoPresupuesto_SerializesAsNumber()
    {
        Assert.Equal(0, (int)EstadoPresupuesto.Borrador);
        Assert.Equal(1, (int)EstadoPresupuesto.Enviado);
        Assert.Equal(2, (int)EstadoPresupuesto.Aceptado);
        Assert.Equal(3, (int)EstadoPresupuesto.Rechazado);
        Assert.Equal(4, (int)EstadoPresupuesto.Anulado);
        Assert.Equal(5, (int)EstadoPresupuesto.Expirado);
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
                UpdateToken => new FirebaseAuthResult(UpdateUid, "update@test.com", EmailVerified: true),
                AnulateToken => new FirebaseAuthResult(AnulateUid, "anulate@test.com", EmailVerified: true),
                AcceptToken => new FirebaseAuthResult(AcceptUid, "accept@test.com", EmailVerified: true),
                NoPermisoToken => new FirebaseAuthResult(NoPermisoUid, "nopermiso@test.com", EmailVerified: true),
                _ => null,
            };

            return Task.FromResult(result);
        }
    }
}
