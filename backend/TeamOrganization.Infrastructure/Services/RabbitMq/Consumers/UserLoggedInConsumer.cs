using System.Text.Json;
using TeamOrganization.Application.Interfaces.Services.Messaging;
using TeamOrganization.Application.Interfaces.Services.Users;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Exceptions;
using TeamOrganization.Domain.Utils;

namespace TeamOrganization.Infrastructure.Services.RabbitMq.Consumers;

public sealed class UserLoggedInConsumer : IMessagingConsumerHandler
{
    private const string ExpectedSource = "team-gateway";
    private const string ExpectedType = "user.logged-in.v1";

    private readonly IUserCommandService _userCommandService;

    public UserLoggedInConsumer(IUserCommandService userCommandService)
    {
        _userCommandService = userCommandService;
    }

    public async Task HandleAsync(ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
    {
        UserLoggedInEnvelope? envelope;

        try
        {
            envelope = JsonSerializer.Deserialize<UserLoggedInEnvelope>(
                body.Span,
                JsonUtils.WebSerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new NonRetryableMessageException(
                "Message body is not a valid user-logged-in event.",
                exception);
        }

        Validate(envelope);

        try
        {
            await _userCommandService.UpdateLastLoginAsync(
                new UpdateLastLoginRequestModel
                {
                    IdentitySubject = envelope!.Data.IdentitySubject,
                    LastLoginAt = envelope.Data.LastLoginAt
                },
                cancellationToken);
        }
        catch (NotFoundException exception)
        {
            throw new NonRetryableMessageException(
                "Login event refers to an unknown organization user.",
                exception);
        }
    }

    private static void Validate(UserLoggedInEnvelope? envelope)
    {
        if (envelope is null
            || envelope.Id == Guid.Empty
            || !string.Equals(envelope.Source, ExpectedSource, StringComparison.Ordinal)
            || !string.Equals(envelope.Type, ExpectedType, StringComparison.Ordinal)
            || envelope.Data is null
            || string.IsNullOrWhiteSpace(envelope.Data.IdentitySubject)
            || envelope.Data.LastLoginAt == default)
        {
            throw new NonRetryableMessageException(
                "The message is not a valid user-logged-in event.");
        }
    }

    private sealed class UserLoggedInEnvelope
    {
        public Guid Id { get; init; }
        public string Source { get; init; } = null!;
        public string Type { get; init; } = null!;
        public UserLoggedInData Data { get; init; } = null!;
    }

    private sealed class UserLoggedInData
    {
        public string IdentitySubject { get; init; } = null!;
        public DateTimeOffset LastLoginAt { get; init; }
    }
}
