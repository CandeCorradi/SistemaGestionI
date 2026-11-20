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
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using Xunit;

namespace SistemaGestion.API.IntegrationTests;

/// <summary>
/// Tests de API de reportes (api-spec §20, BR-140): autorización
/// REPORTES_VIEW/REPORTES_EXPORT, preview JSON, 404 por clave desconocida,
/// 400 por filtros/formato inválidos y descarga exportada con
/// Content-Disposition.
/// </summary>
public sealed class ReportsApiTests : IClassFixture<ReportsApiTests.Factory>
{
    private const string AdminToken = "reports-admin-token";
    private const string ViewToken = "reports-view-token";
    private const string NoPermisoToken = "reports-nopermiso-token";

    private const string AdminUid = "uid-reports-admin";
    private const string ViewUid = "uid-reports-view";
    private const string NoPermisoUid = "uid-reports-nopermiso";

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
                permisos.EfectivosPorUid[ViewUid] = new List<string> { Permisos.ReportesView };
                permisos.EfectivosPorUid[NoPermisoUid] = new List<string> { Permisos.PagosView };
                services.AddSingleton<IPermisoRepository>(permisos);

                var usuarios = new FakeUsuarioRepository();
                usuarios.Seed(AdminUid, "Admin", "Reportes");
                usuarios.Seed(ViewUid, "Solo", "Ver");
                usuarios.Seed(NoPermisoUid, "Sin", "Permisos");
                services.AddSingleton<IUsuarioRepository>(usuarios);

                var reportes = new FakeReportRepository();
                var cliente = Cliente.Crear("Ana", "García", "ana@test.com");
                cliente.Id = 7;
                var pedido = Pedido.Crear(cliente.Id, 1, 2000m,
                    new DateTime(2026, 10, 5, 10, 0, 0, DateTimeKind.Utc));
                pedido.Id = 1;
                reportes.Pedidos.Add(pedido);
                reportes.SumasPagadas[1] = 500m;
                services.AddSingleton<IReportRepository>(reportes);
            });
        }
    }

    private readonly Factory _factory;

    private readonly HttpClient _client;

    public ReportsApiTests(Factory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private HttpRequestMessage AuthenticatedRequest(HttpMethod method, string url, string token = AdminToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    [Fact]
    public async Task Get_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/reports/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithoutReportPermission_ReturnsForbidden()
    {
        var response = await _client.SendAsync(
            AuthenticatedRequest(HttpMethod.Get, "/api/v1/reports/orders", NoPermisoToken));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_Orders_ReturnsDocumentWithRows()
    {
        var response = await _client.SendAsync(
            AuthenticatedRequest(HttpMethod.Get, "/api/v1/reports/orders"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("orders", json.RootElement.GetProperty("key").GetString());
        Assert.Equal("Reporte de pedidos", json.RootElement.GetProperty("title").GetString());
        var rows = json.RootElement.GetProperty("rows");
        Assert.Equal(1, rows.GetArrayLength());
        Assert.Equal("#1", rows[0].GetProperty("cells").GetProperty("numero").GetString());
        Assert.Equal(500m, rows[0].GetProperty("cells").GetProperty("pagado").GetDecimal());
        Assert.Equal(1500m, rows[0].GetProperty("cells").GetProperty("saldo").GetDecimal());
    }

    [Fact]
    public async Task Get_UnknownReport_ReturnsNotFound()
    {
        var response = await _client.SendAsync(
            AuthenticatedRequest(HttpMethod.Get, "/api/v1/reports/desconocido"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_InvalidRange_ReturnsBadRequest()
    {
        var response = await _client.SendAsync(
            AuthenticatedRequest(HttpMethod.Get,
                "/api/v1/reports/orders?from=2026-10-10&to=2026-10-01"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Export_WithoutExportPermission_ReturnsForbidden()
    {
        var response = await _client.SendAsync(
            AuthenticatedRequest(HttpMethod.Get,
                "/api/v1/reports/orders/export?format=csv", ViewToken));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Export_Xlsx_ReturnsFileDownload()
    {
        var response = await _client.SendAsync(
            AuthenticatedRequest(HttpMethod.Get,
                "/api/v1/reports/orders/export?format=xlsx"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("reporte-orders-", response.Content.Headers.ContentDisposition?.FileName);
        Assert.Contains(".xlsx", response.Content.Headers.ContentDisposition?.FileName);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public async Task Export_Csv_ReturnsCsvFile()
    {
        var response = await _client.SendAsync(
            AuthenticatedRequest(HttpMethod.Get,
                "/api/v1/reports/orders/export?format=csv"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("csv", response.Content.Headers.ContentType?.MediaType);
        var csv = await response.Content.ReadAsStringAsync();
        Assert.Contains("Pedido", csv);
    }

    [Fact]
    public async Task Export_Pdf_ReturnsPdfFile()
    {
        var response = await _client.SendAsync(
            AuthenticatedRequest(HttpMethod.Get,
                "/api/v1/reports/orders/export?format=pdf"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 100);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public async Task Export_InvalidFormat_ReturnsBadRequest()
    {
        var response = await _client.SendAsync(
            AuthenticatedRequest(HttpMethod.Get,
                "/api/v1/reports/orders/export?format=docx"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_Inventory_RequiresOnlyReportPermission()
    {
        var response = await _client.SendAsync(
            AuthenticatedRequest(HttpMethod.Get, "/api/v1/reports/inventory", ViewToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
                NoPermisoToken => new FirebaseAuthResult(NoPermisoUid, "nopermiso@test.com", EmailVerified: true),
                _ => null,
            };

            return Task.FromResult(result);
        }
    }
}
