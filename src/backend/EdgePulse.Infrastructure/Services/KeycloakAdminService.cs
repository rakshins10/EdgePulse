using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EdgePulse.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EdgePulse.Infrastructure.Services;

/// <summary>
/// Keycloak Admin REST implementation of <see cref="IIdentityAdminService"/>.
///
/// Authenticates against the master realm with the admin-cli client using
/// credentials from configuration (Keycloak:AdminUsername / AdminPassword —
/// admin/admin in local dev). In production, prefer a dedicated service
/// account with just the realm-management `manage-users` role.
///
/// EdgePulse's role / tenant / mill / area scoping lives in USER ATTRIBUTES;
/// protocol mappers copy them into JWT claims at login.
/// </summary>
public class KeycloakAdminService : IIdentityAdminService
{
    private readonly HttpClient _http;
    private readonly ILogger<KeycloakAdminService> _logger;
    private readonly string _baseUrl;   // e.g. http://localhost:8080
    private readonly string _realm;     // e.g. edgepulse
    private readonly string _adminUser;
    private readonly string _adminPassword;

    private string? _cachedToken;
    private DateTime _tokenExpiresAt = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public KeycloakAdminService(
        HttpClient http,
        IConfiguration configuration,
        ILogger<KeycloakAdminService> logger)
    {
        _http = http;
        _logger = logger;

        // Authority looks like http://host:8080/realms/edgepulse
        var authority = configuration["Keycloak:Authority"]
            ?? throw new InvalidOperationException("Keycloak:Authority not configured.");
        var idx = authority.IndexOf("/realms/", StringComparison.OrdinalIgnoreCase);
        _baseUrl = idx > 0 ? authority[..idx] : authority;
        _realm = idx > 0 ? authority[(idx + "/realms/".Length)..].TrimEnd('/') : "edgepulse";

        _adminUser = configuration["Keycloak:AdminUsername"] ?? "admin";
        _adminPassword = configuration["Keycloak:AdminPassword"] ?? "admin";
    }

    // ── IIdentityAdminService ────────────────────────────────────────────────

    public async Task<List<IdentityUser>> GetUsersAsync(CancellationToken ct)
    {
        var users = await SendAsync<List<KcUser>>(
            HttpMethod.Get, $"users?max=500&briefRepresentation=false", null, ct);
        return (users ?? []).Select(Map).ToList();
    }

    public async Task<IdentityUser?> GetUserAsync(string id, CancellationToken ct)
    {
        try
        {
            var user = await SendAsync<KcUser>(HttpMethod.Get, $"users/{id}", null, ct);
            return user is null ? null : Map(user);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<string> CreateUserAsync(CreateIdentityUser user, CancellationToken ct)
    {
        var body = new
        {
            username = user.Email,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            enabled = true,
            emailVerified = true,
            attributes = BuildAttributes(user.Role, user.TenantId, user.MillId, user.AreaIds),
            credentials = new[]
            {
                new { type = "password", value = user.TemporaryPassword, temporary = true }
            }
        };

        var response = await SendRawAsync(HttpMethod.Post, "users", body, ct);

        // Keycloak returns 201 with a Location header ending in the new id
        var location = response.Headers.Location?.ToString()
            ?? throw new InvalidOperationException("Keycloak did not return the new user id.");
        return location.TrimEnd('/').Split('/')[^1];
    }

    public async Task UpdateUserRoleAsync(
        string id, string role, Guid? millId, IReadOnlyList<Guid> areaIds,
        CancellationToken ct)
    {
        // Keycloak's PUT /users/{id} clears profile fields that are absent
        // from the representation — always send the FULL user back with only
        // the scoping attributes changed.
        var existing = await SendAsync<KcUser>(HttpMethod.Get, $"users/{id}", null, ct)
            ?? throw new InvalidOperationException($"User {id} not found in Keycloak.");

        var tenantId = ExtractGuid(existing.Attributes, "tenantId");
        var body = FullRepresentation(existing) with
        {
            Attributes = BuildAttributes(role, tenantId, millId, areaIds)
        };
        await SendRawAsync(HttpMethod.Put, $"users/{id}", body, ct);
    }

    public async Task SetUserEnabledAsync(string id, bool enabled, CancellationToken ct)
    {
        var existing = await SendAsync<KcUser>(HttpMethod.Get, $"users/{id}", null, ct)
            ?? throw new InvalidOperationException($"User {id} not found in Keycloak.");

        var body = FullRepresentation(existing) with { Enabled = enabled };
        await SendRawAsync(HttpMethod.Put, $"users/{id}", body, ct);
    }

    /// <summary>
    /// Everything Keycloak would otherwise null out on a partial PUT.
    /// </summary>
    private static KcUserUpdate FullRepresentation(KcUser existing) => new()
    {
        Username = existing.Username,
        Email = existing.Email,
        FirstName = existing.FirstName,
        LastName = existing.LastName,
        Enabled = existing.Enabled,
        EmailVerified = true,
        Attributes = existing.Attributes ?? new Dictionary<string, string[]>(),
    };

    public async Task ResetPasswordAsync(
        string id, string temporaryPassword, CancellationToken ct)
    {
        await SendRawAsync(
            HttpMethod.Put, $"users/{id}/reset-password",
            new { type = "password", value = temporaryPassword, temporary = true },
            ct);
    }

    // ── plumbing ─────────────────────────────────────────────────────────────

    private static Dictionary<string, string[]> BuildAttributes(
        string role, Guid? tenantId, Guid? millId, IReadOnlyList<Guid> areaIds)
    {
        var attributes = new Dictionary<string, string[]>
        {
            ["role"] = [role],
        };
        if (tenantId is not null) attributes["tenantId"] = [tenantId.Value.ToString()];
        if (millId is not null) attributes["millId"] = [millId.Value.ToString()];
        if (areaIds.Count > 0)
            attributes["areaIds"] = areaIds.Select(a => a.ToString()).ToArray();
        return attributes;
    }

    private static IdentityUser Map(KcUser u) => new(
        u.Id ?? string.Empty,
        u.Username ?? string.Empty,
        u.Email,
        u.FirstName,
        u.LastName,
        u.Enabled,
        u.Attributes?.GetValueOrDefault("role")?.FirstOrDefault(),
        ExtractGuid(u.Attributes, "tenantId"),
        ExtractGuid(u.Attributes, "millId"),
        u.Attributes?.GetValueOrDefault("areaIds")?
            .Select(v => Guid.TryParse(v, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToList() ?? []);

    private static Guid? ExtractGuid(Dictionary<string, string[]>? attributes, string key)
        => attributes is not null &&
           attributes.TryGetValue(key, out var values) &&
           Guid.TryParse(values.FirstOrDefault(), out var g)
            ? g : null;

    private async Task<T?> SendAsync<T>(
        HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var response = await SendRawAsync(method, path, body, ct);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
    }

    private async Task<HttpResponseMessage> SendRawAsync(
        HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var request = new HttpRequestMessage(
            method, $"{_baseUrl}/admin/realms/{_realm}/{path}");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer", await GetAdminTokenAsync(ct));
        if (body is not null)
            request.Content = JsonContent.Create(body);

        var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Keycloak admin call {Method} {Path} failed: {Status} {Detail}",
                method, path, (int)response.StatusCode, detail);
            throw new HttpRequestException(
                $"Keycloak admin call failed ({(int)response.StatusCode}).",
                null, response.StatusCode);
        }
        return response;
    }

    private async Task<string> GetAdminTokenAsync(CancellationToken ct)
    {
        if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiresAt)
            return _cachedToken;

        await _tokenLock.WaitAsync(ct);
        try
        {
            if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiresAt)
                return _cachedToken;

            var response = await _http.PostAsync(
                $"{_baseUrl}/realms/master/protocol/openid-connect/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "password",
                    ["client_id"] = "admin-cli",
                    ["username"] = _adminUser,
                    ["password"] = _adminPassword,
                }), ct);
            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync<KcToken>(cancellationToken: ct)
                ?? throw new InvalidOperationException("Empty token response from Keycloak.");

            _cachedToken = token.AccessToken;
            // refresh 30 s before actual expiry
            _tokenExpiresAt = DateTime.UtcNow.AddSeconds(Math.Max(30, token.ExpiresIn - 30));
            return _cachedToken!;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private sealed class KcToken
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    }

    private sealed class KcUser
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("username")] public string? Username { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("firstName")] public string? FirstName { get; set; }
        [JsonPropertyName("lastName")] public string? LastName { get; set; }
        [JsonPropertyName("enabled")] public bool Enabled { get; set; }
        [JsonPropertyName("attributes")] public Dictionary<string, string[]>? Attributes { get; set; }
    }

    private sealed record KcUserUpdate
    {
        [JsonPropertyName("username")] public string? Username { get; init; }
        [JsonPropertyName("email")] public string? Email { get; init; }
        [JsonPropertyName("firstName")] public string? FirstName { get; init; }
        [JsonPropertyName("lastName")] public string? LastName { get; init; }
        [JsonPropertyName("enabled")] public bool Enabled { get; init; }
        [JsonPropertyName("emailVerified")] public bool EmailVerified { get; init; }
        [JsonPropertyName("attributes")] public Dictionary<string, string[]> Attributes { get; init; } = new();
    }
}
