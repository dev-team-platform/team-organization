namespace TeamOrganization.Domain.Constants.Notifications;

public static class NotificationContents
{
    public static class CreatedUser
    {
        public const string Title = "User created";
        public const string SuccessMessage = "A new user with display name {displayName} and email {email} has been created successfully.";
    }
}