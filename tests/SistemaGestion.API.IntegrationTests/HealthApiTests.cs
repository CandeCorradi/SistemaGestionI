using System.Net;
using System.Text.Json;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using SistemaGestion.API;
using Microsoft.AspNetCore.Hosting;

namespace SistemaGestion.API.IntegrationTests;

public sealed class HealthApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"))
            .CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsCheckForPostgres()
    {
        var response = await _client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        // Si la respuesta es JSON, busca el nodo "postgres"
        if (body.StartsWith("{"))
        {
            using var doc = JsonDocument.Parse(body);
            Assert.True(doc.RootElement.TryGetProperty("postgres", out _),
                "La respuesta JSON no contiene el nodo 'postgres'.");
            return;
        }

        // Si es texto plano (ej. "Healthy"), verificamos que contenga la subcadena
        Assert.Contains("postgres", body);
    }

    [Fact]
    public async Task GetOpenApi_ReturnsDocumentation_InDevelopment()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"openapi\"", body);
    }
}