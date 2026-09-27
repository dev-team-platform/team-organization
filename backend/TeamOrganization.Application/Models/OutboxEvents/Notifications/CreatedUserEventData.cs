using TeamOrganization.Domain.Entities;

namespace TeamOrganization.Application.Models.OutboxEvents.Notifications;

public class CreatedUserEventData
{
    public required Guid UserId { get; init; }
    public required string IdentitySubject { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string DisplayName { get; init; }

    public static CreatedUserEventData FromEntity(User user)
    {
        return new CreatedUserEventData
        {
            UserId = user.Id,
            IdentitySubject = user.IdentitySubject,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName ?? string.Empty,
            DisplayName = user.DisplayName
        };
    }
}