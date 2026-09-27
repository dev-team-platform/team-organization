using System.Collections.Frozen;
using System.Runtime.InteropServices;

namespace TeamOrganization.Application.Constants.Notifications;

public static class NotificationContents
{
    public static class CreatedUser
    {
        public const string Title = "User created";
        public const string SuccessMessage = "A new user with display name {0} and email {1} has been created successfully.";
    }
}