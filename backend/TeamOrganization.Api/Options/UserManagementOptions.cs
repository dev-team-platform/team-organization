namespace TeamOrganization.Api.Options;

public class UserManagementOptions
{
    public const string SectionName = "UserManagement";
    public string DefaultPassword { get; set; } = null!;
}