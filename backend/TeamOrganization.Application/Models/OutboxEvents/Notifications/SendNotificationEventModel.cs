using System.Text.Json;

namespace TeamOrganization.Application.Models.OutboxEvents.Notifications;

public class SendNotificationEventModel
{
    public string SourceApp { get; } = "team-organization";
    public required string EventType { get; init; }
    public required Guid CreatedById { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public NotificationEventModel? Notification { get; init; }
    public EmailEventModel? Email { get; init; }
}

public class NotificationEventModel
{
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required IReadOnlyList<NotificationRecipientEventModel> Recipients { get; init; }
    public JsonDocument? Data { get; init; }
}

public class NotificationRecipientEventModel
{
    public required Guid UserId { get; init; }
    public required string IdentitySubject { get; init; }
}

public class EmailEventModel
{
    public string? FromEmail { get; init; }
    public string? FromName { get; init; }
    public required IReadOnlyList<EmailRecipientEventModel> Recipients { get; init; }
    public required string TemplateKey { get; init; }
    public JsonDocument? Data { get; init; }
}

public class EmailRecipientEventModel
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
}
