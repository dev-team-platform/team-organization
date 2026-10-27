namespace TeamOrganization.Domain.Constants.Uam;

public static class PC
{
    public static class UserSelf
    {
        public const string Read = "user.self.read";
        public const string Update = "user.self.update";
    }

    public static class User
    {
        public const string Read = "user.read";
        public const string Create = "user.create";
        public const string Update = "user.update";
    }

    public static class Admin
    {
        public const string Read = "admin.read";
        public const string Create = "admin.create";
        public const string Update = "admin.update";
    }
}