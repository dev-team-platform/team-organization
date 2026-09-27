using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TeamOrganization.Api.Options;
using TeamOrganization.Domain.Exceptions;

namespace TeamOrganization.Api.Services;

public sealed class KeycloakService : IKeycloakService
{
    public const string HttpClientName = "KeycloakAdminApi";
    private static readonly TimeSpan TokenRefreshSkew = TimeSpan.FromSeconds(15);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<AuthOptions> _authOptions;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _expiresAt;

    public KeycloakService(
        IHttpClientFactory httpClientFactory,
        IOptions<AuthOptions> authOptions)
    {
        _httpClientFactory = httpClientFactory;
        _authOptions = authOptions;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (HasUsableToken())
        {
            return _accessToken!;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (HasUsableToken())
            {
                return _accessToken!;
            }

            var keycloak = _authOptions.Value.KeycloakForAdminApi;
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "protocol/openid-connect/token")
            {
                Content = new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", keycloak.ClientId),
                    new KeyValuePair<string, string>("client_secret", keycloak.ClientSecret)
                ])
            };

            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken);
            if (string.IsNullOrWhiteSpace(token?.AccessToken))
            {
                throw new InvalidOperationException(
                    "Keycloak Admin API token response did not contain an access token.");
            }

            _accessToken = token.AccessToken;
            _expiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn ?? 60);

            return _accessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task<string> CreateNewUserAsync(
        string accessToken,
        KeycloakNewUser user,
        string defaultPassword,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, GetAdminUsersEndpoint())
        {
            Headers =
            {
                Authorization = new AuthenticationHeaderValue("Bearer", accessToken)
            },
            Content = JsonContent.Create(new
            {
                username = user.Username,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                enabled = true,
                credentials = new[]
                {
                    new
                    {
                        type = "password",
                        value = defaultPassword,
                        temporary = true
                    }
                }
            })
        };

        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new ConflictException("A Keycloak user with the same username or email already exists.");
        }

        response.EnsureSuccessStatusCode();

        var identitySubject = GetIdentitySubject(response.Headers.Location);
        if (string.IsNullOrWhiteSpace(identitySubject))
        {
            throw new InvalidOperationException(
                "Keycloak created the user but did not return its identity subject.");
        }

        return identitySubject;
    }

    private Uri GetAdminUsersEndpoint()
    {
        var authority = new Uri(_authOptions.Value.KeycloakForAdminApi.Authority, UriKind.Absolute);
        var path = authority.AbsolutePath.TrimEnd('/');
        const string realmMarker = "/realms/";
        var realmMarkerIndex = path.LastIndexOf(realmMarker, StringComparison.OrdinalIgnoreCase);

        if (realmMarkerIndex < 0 || realmMarkerIndex + realmMarker.Length >= path.Length)
        {
            throw new InvalidOperationException(
                "Auth:KeycloakForAdminApi:Authority must contain /realms/{realm}.");
        }

        var realm = path[(realmMarkerIndex + realmMarker.Length)..];
        var keycloakBasePath = path[..realmMarkerIndex];
        var builder = new UriBuilder(authority.Scheme, authority.Host, authority.Port)
        {
            Path = $"{keycloakBasePath}/admin/realms/{Uri.EscapeDataString(realm)}/users"
        };

        return builder.Uri;
    }

    private bool HasUsableToken() =>
        !string.IsNullOrWhiteSpace(_accessToken)
        && _expiresAt > DateTimeOffset.UtcNow.Add(TokenRefreshSkew);

    private static string? GetIdentitySubject(Uri? location)
    {
        var path = location is null
            ? null
            : (location.IsAbsoluteUri ? location.AbsolutePath : location.OriginalString).TrimEnd('/');
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var userId = path[(path.LastIndexOf('/') + 1)..];
        return Uri.UnescapeDataString(userId);
    }

    private sealed class KeycloakTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; init; }
    }
}
