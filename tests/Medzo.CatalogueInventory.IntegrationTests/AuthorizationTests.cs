using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Medzo.CatalogueInventory.IntegrationTests;

public sealed class AuthorizationTests : IClassFixture<AuthorizationTests.ApiFactory>
{
    private const string Secret = "integration-test-signing-secret-32-bytes-minimum";
    private readonly HttpClient _client;

    public AuthorizationTests(ApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Catalogue_WithoutAccessToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/catalogue/medicines", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddMedicine_AsPharmacist_ReturnsForbidden()
    {
        UseToken("Pharmacist");

        var response = await _client.PostAsync("/api/catalogue/medicines", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task LowStock_AsPharmacist_ReturnsForbidden()
    {
        UseToken("Pharmacist");

        var response = await _client.GetAsync("/api/inventory/items/low-stock", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMedicine_AsPharmacist_ReturnsForbidden()
    {
        UseToken("Pharmacist");
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/catalogue/medicines/{Guid.NewGuid()}");
        request.Headers.TryAddWithoutValidation("If-Match", "0");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Catalogue_WithWrongAudience_ReturnsUnauthorized()
    {
        UseToken("Pharmacist", "AnotherClient");

        var response = await _client.GetAsync("/api/catalogue/medicines", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private void UseToken(string role, string audience = "MedzoClient")
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "MedzoAuthService",
            audience: audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, role)
            ],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
    }

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:CatalogueInventory", "Server=localhost;Database=authorization_tests;User Id=test;Password=not-used;TrustServerCertificate=True");
            builder.UseSetting("Database:Provider", "SqlServer");
            builder.UseSetting("SeedDemoData:Enabled", "false");
            builder.UseSetting("Jwt:Secret", Secret);
            builder.UseSetting("Jwt:Issuer", "MedzoAuthService");
            builder.UseSetting("Jwt:Audience", "MedzoClient");
            builder.ConfigureServices(services => services.RemoveAll<IHostedService>());
        }
    }
}
