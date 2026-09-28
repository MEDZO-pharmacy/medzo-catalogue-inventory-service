using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Medzo.CatalogueInventory.Api.Configuration;
using Medzo.CatalogueInventory.Api.ExceptionHandling;
using Medzo.CatalogueInventory.Application.Catalogue;
using Medzo.CatalogueInventory.Application.Common;
using Medzo.CatalogueInventory.Application.Inventory;
using Medzo.CatalogueInventory.Infrastructure;
using Medzo.CatalogueInventory.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

LocalEnvironmentFile.LoadFromCurrentDirectory();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddScoped<ICatalogueService, CatalogueService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddInfrastructure(builder.Configuration);

var secret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < 32 ||
    secret.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        "Replace Jwt__Secret in the repository-root .env file with the exact Auth service signing secret (at least 32 bytes).");
}

var issuer = builder.Configuration["Jwt:Issuer"];
if (string.IsNullOrWhiteSpace(issuer)) throw new InvalidOperationException("Set Jwt__Issuer.");
var audience = builder.Configuration["Jwt:Audience"];
if (string.IsNullOrWhiteSpace(audience)) throw new InvalidOperationException("Set Jwt__Audience.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        RequireExpirationTime = true,
        RequireSignedTokens = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
        RoleClaimType = ClaimTypes.Role,
        ClockSkew = TimeSpan.Zero
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CatalogueRead", policy => policy.RequireRole("Pharmacist", "InventoryManager", "Admin"));
    options.AddPolicy("InventoryRead", policy => policy.RequireRole("Pharmacist", "InventoryManager", "Admin"));
    options.AddPolicy("InventoryEdit", policy => policy.RequireRole("Pharmacist", "InventoryManager", "Admin"));
    options.AddPolicy("InventoryManage", policy => policy.RequireRole("InventoryManager", "Admin"));
});

var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

// Run migrations against the same Azure SQL connection used by the API. This
// is opt-in and intended for the single-replica production Container App.
// EF's SQL retry policy handles Azure SQL Serverless resume delays.
if (builder.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<CatalogueInventoryDbContext>();
    await database.Database.MigrateAsync();
}

if (string.Equals(builder.Configuration["Database:Provider"], "Sqlite", StringComparison.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<CatalogueInventoryDbContext>();
    await EnsureSqliteReceiptColumnsAsync(database);
}
if (app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("SeedDemoData:Enabled"))
    await DemoDataSeeder.SeedAsync(app.Services);

app.UseExceptionHandler();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();

static async Task EnsureSqliteReceiptColumnsAsync(CatalogueInventoryDbContext database)
{
    var connection = database.Database.GetDbConnection();
    await connection.OpenAsync();
    try
    {
        await using var columnsCommand = connection.CreateCommand();
        columnsCommand.CommandText = "PRAGMA table_info(stock_movements);";
        await using var reader = await columnsCommand.ExecuteReaderAsync();
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync()) columns.Add(reader.GetString(1));
        await reader.DisposeAsync();

        if (!columns.Contains("PharmacistUsername"))
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "ALTER TABLE stock_movements ADD COLUMN PharmacistUsername TEXT NULL;";
            await command.ExecuteNonQueryAsync();
        }
        if (!columns.Contains("UnitPrice"))
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "ALTER TABLE stock_movements ADD COLUMN UnitPrice NUMERIC NULL;";
            await command.ExecuteNonQueryAsync();
        }
    }
    finally
    {
        await connection.CloseAsync();
    }
}

public partial class Program;