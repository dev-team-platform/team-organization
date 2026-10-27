namespace TeamOrganization.Application.Models.Messaging.Publishing.Notifications;

public class DisableUserOtpEmailData
{
    public required string RecipientDisplayName { get; init; }
    public required string TargetDisplayName { get; init; }
    public required string TargetUsername { get; init; }
    public required string CurrentStatus { get; init; }
    public required string OtpCode { get; init; }
    public required int OtpExpiryMinutes { get; init; }
    public required int CurrentYear { get; init; }
}
