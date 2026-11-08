namespace TeamOrganization.Domain.Constants.Notifications;

public static class NotificationContents
{
    public static class CreatedUser
    {
        public const string Title = "User created";
        public const string SuccessContent = "A new user with display name {displayName} and email {email} has been created successfully.";
    }

    public static class Export
    {
        public static class Status
        {
            public const string InProgress = "InProgress";
            public const string Successful = "Successful";
            public const string Failed = "Failed";
        }

        public static class Title
        {
            public const string InProgress = "Exporting file";
            public const string Successful = "Exporting file successfully";
            public const string Failed = "Exporting file failed";
        }

        public static class Content
        {
            public const string InProgress = "Exporting {fileName} is in progress.";
            public const string Successful = "Exporting {fileName} is successful. Click here to download.";
            public const string Failed = "Exporting {fileName} is failed. Please try again.";
        }
    }
}
