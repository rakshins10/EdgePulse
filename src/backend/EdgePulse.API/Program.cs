using EdgePulse.Application;
using EdgePulse.Infrastructure;
using EdgePulse.Infrastructure.Persistence;
using EdgePulse.Infrastructure.Persistence.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

// MongoDB.Driver 3.x: Guids are stored as strings by TelemetryProcessor — register
// the matching serializer here so reads deserialize correctly.
BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));

var builder = WebApplication.CreateBuilder(args);

// Fail fast if a secret is still the committed placeholder. Real values come
// from 'dotnet user-secrets' (Development) or environment variables
// (ConnectionStrings__DefaultConnection, Keycloak__ClientSecret, ...) — never git.
foreach (var key in new[] { "ConnectionStrings:DefaultConnection", "ConnectionStrings:MongoDB", "Keycloak:ClientSecret" })
{
    var value = builder.Configuration[key];
    if (string.IsNullOrWhiteSpace(value) || value.Contains("<SET-VIA-"))
        throw new InvalidOperationException(
            $"{key} is not configured. Set it via 'dotnet user-secrets' (Development) " +
            $"or the {key.Replace(":", "__")} environment variable " +
            "(use double underscore: Section__Key). See docs/guides/02-configuration-guide.md.");
}

// ----------------------------------------------------------------
// Authentication — Keycloak JWT Bearer
// ----------------------------------------------------------------
var keycloakAuthority = builder.Configuration["Keycloak:Authority"]!;
var keycloakAudience  = builder.Configuration["Keycloak:Audience"]!;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = keycloakAuthority;
        options.Audience  = keycloakAudience;

        // Allow HTTP in development (Keycloak runs on http://localhost:8080)
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

        // Keep JWT claim names as-is from Keycloak (don't map "role" → ClaimTypes.Role URI).
        // With MapInboundClaims = true (the default), the JWT middleware renames claim types
        // to their long WS-* URIs, which breaks Claim("role") lookups in CurrentUserService.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            // Keycloak puts the role claim under "role" (our custom User Attribute mapper)
            RoleClaimType = "role",
            // Keycloak uses "sub" as the name identifier
            NameClaimType = "sub"
        };
    });

// ----------------------------------------------------------------
// Add services
// ----------------------------------------------------------------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        // Accept and emit enums as strings (e.g. "Cloud" not 0).
        // Without this, PUT/POST requests with string enum values return 400.
        opts.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();

// ----------------------------------------------------------------
// Swagger — with Bearer token "Authorize" button
// ----------------------------------------------------------------
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "EdgePulse API",
        Version     = "v1",
        Description = "Industrial IoT Device Management Platform"
    });

    // Define the Bearer security scheme
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Paste your Keycloak access token here.\n\nGet one with:\ncurl -s -X POST http://localhost:8080/realms/edgepulse/protocol/openid-connect/token -d 'grant_type=password&client_id=edgepulse-api&client_secret=<edgepulse-api-client-secret>&username=superadmin&password=Test@1234' | jq -r .access_token"
    });

    // Apply the Bearer scheme globally to all operations
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ----------------------------------------------------------------
// --seed flag: run demo data seeding then exit
// ----------------------------------------------------------------
if (args.Contains("--seed"))
{
    using var scope = app.Services.CreateScope();
    var db     = scope.ServiceProvider.GetRequiredService<EdgePulseDbContext>();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILogger<DemoSeedService>>();
    var seeder = new DemoSeedService(db, logger);
    await seeder.SeedAsync();
    Console.WriteLine("Demo seed complete. Exiting.");
    return;
}

// ----------------------------------------------------------------
// Middleware pipeline
// ----------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EdgePulse API v1");
        c.RoutePrefix = "swagger";
    });
}

// ── Health endpoint (unauthenticated) ────────────────────────────────────────
// Used by HAProxy to determine whether this instance is healthy.
// Returns 200 {"status":"healthy"} — no JWT required.
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
   .AllowAnonymous();

app.UseMiddleware<EdgePulse.API.Middleware.ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();

// Order matters: Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
