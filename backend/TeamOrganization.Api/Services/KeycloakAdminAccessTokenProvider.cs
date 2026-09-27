using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TeamOrganization.Api.Options;

namespace TeamOrganization.Api.Services;

public sealed class KeycloakAdminAccessTokenProvider : IKeycloakAdminAccessTokenProvider
{
    public const string HttpClientName = "KeycloakAdminApi";
    private static readonly TimeSpan TokenRefreshSkew = TimeSpan.FromSeconds(15);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<AuthOptions> _authOptions;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _expiresAt;

    public KeycloakAdminAccessTokenProvider(
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

    private bool HasUsableToken() =>
        !string.IsNullOrWhiteSpace(_accessToken)
        && _expiresAt > DateTimeOffset.UtcNow.Add(TokenRefreshSkew);

    private sealed class KeycloakTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; init; }
    }
}
