using AwesomeAssertions;
using System.Net;
using Moq;
using Moq.Protected;
using TeamOrganization.Application.Models.IdentityProviders;
using TeamOrganization.Domain.Exceptions;
using TeamOrganization.Infrastructure.Options;
using TeamOrganization.Infrastructure.Services.IdentityProviders;
using Xunit;

namespace TeamOrganization.Infrastructure.Tests.Services.IdentityProviders;

public class KeycloakServiceTests
{
    private const string Authority = "https://identity.example.test/realms/company";
    private readonly Mock<HttpMessageHandler> _handler = new(MockBehavior.Strict);
    private readonly Mock<IHttpClientFactory> _clientFactory = new();
    private readonly KeycloakService _service;

    public KeycloakServiceTests()
    {
        var client = new HttpClient(_handler.Object)
        {
            BaseAddress = new Uri(Authority + "/")
        };
        _clientFactory.Setup(factory => factory.CreateClient(KeycloakService.HttpClientName)).Returns(client);
        _service = new KeycloakService(_clientFactory.Object, Microsoft.Extensions.Options.Options.Create(new AuthOptions
        {
            KeycloakForAdminApi = new KeycloakForAdminApiOptions
            {
                Authority = Authority, ClientId = "client-id", ClientSecret = "client-secret"
            }
        }));
    }

    [Fact]
    public async Task GetAccessTokenAsync_ShouldReuseCachedToken_WhenTokenIsUsable()
    {
        SetupResponse(
            request => request.Method == HttpMethod.Post && request.RequestUri!.AbsoluteUri ==
                "https://identity.example.test/realms/company/protocol/openid-connect/token",
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"access_token\":\"cached-token\",\"expires_in\":120}")
            });

        (await _service.GetAccessTokenAsync()).Should().Be("cached-token");
        (await _service.GetAccessTokenAsync()).Should().Be("cached-token");

        VerifySend(request => request.Method == HttpMethod.Post, Times.Once());
    }

    [Fact]
    public async Task GetAccessTokenAsync_ShouldThrowInvalidOperationException_WhenResponseHasNoToken()
    {
        SetupResponse(
            request => request.Method == HttpMethod.Post,
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });

        Func<Task> act = () => _service.GetAccessTokenAsync();
        var exception = (await act.Should().ThrowAsync<InvalidOperationException>()).Which;

        exception.Message.Should().Contain("did not contain an access token");
    }

    [Fact]
    public async Task CreateNewUserAsync_ShouldReturnSubject_WhenLocationHeaderContainsUserId()
    {
        SetupResponse(
            request => request.Method == HttpMethod.Post && request.RequestUri!.AbsoluteUri ==
                "https://identity.example.test/admin/realms/company/users",
            new HttpResponseMessage(HttpStatusCode.Created)
            {
                Headers = { Location = new Uri("https://identity.example.test/admin/realms/company/users/user-123") }
            });

        var subject = await _service.CreateNewUserAsync(
            "admin-token",
            new IdentityProviderNewUserRequestModel
            {
                Username = "alice", Email = "alice@example.test", FirstName = "Alice", LastName = "Smith"
            },
            "temporary-password");

        subject.Should().Be("user-123");
        VerifySend(request => request.Headers.Authorization!.Parameter == "admin-token", Times.Once());
    }

    [Fact]
    public async Task CreateNewUserAsync_ShouldThrowConflictException_WhenIdentityProviderReturnsConflict()
    {
        SetupResponse(
            request => request.Method == HttpMethod.Post,
            new HttpResponseMessage(HttpStatusCode.Conflict));

        Func<Task> act = () => _service.CreateNewUserAsync(
            "token", new IdentityProviderNewUserRequestModel
            {
                Username = "alice", Email = "a@example.test", FirstName = "Alice"
            }, "pw");
        var exception = (await act.Should().ThrowAsync<ConflictException>()).Which;

        exception.Message.Should().Contain("same username or email");
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldCompleteSuccessfully_WhenUserDoesNotExist()
    {
        SetupResponse(
            request => request.Method == HttpMethod.Delete && request.RequestUri!.AbsoluteUri.EndsWith("/user-123"),
            new HttpResponseMessage(HttpStatusCode.NotFound));

        await _service.DeleteUserAsync("token", "user-123");

        VerifySend(request => request.Method == HttpMethod.Delete, Times.Once());
    }

    private void SetupResponse(Func<HttpRequestMessage, bool> requestPredicate, HttpResponseMessage response)
    {
        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(request => requestPredicate(request)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void VerifySend(Func<HttpRequestMessage, bool> requestPredicate, Times times)
    {
        _handler.Protected().Verify(
            "SendAsync",
            times,
            ItExpr.Is<HttpRequestMessage>(request => requestPredicate(request)),
            ItExpr.IsAny<CancellationToken>());
    }
}
