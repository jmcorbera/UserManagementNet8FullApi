extern alias Api;
using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Program = Api::UserManagement.API.Program;

namespace UserManagement.IntegrationTests;

/// <summary>
/// Milestone 01-setup: verify API starts and responds (health and root endpoint).
/// </summary>
public class HealthCheckTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthCheckTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Root_endpoint_returns_OK()
    {
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Swagger UI");
    }

    [Fact]
    public async Task Health_endpoint_returns_Healthy()
    {
        var response = await _client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
