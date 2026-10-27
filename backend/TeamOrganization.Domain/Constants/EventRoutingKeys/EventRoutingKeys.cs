namespace TeamOrganization.Domain.Constants.EventRoutingKeys;

public class EventRoutingKeys
{
    public const string UserCreated = "user.created.v1";
    public const string UserUpdated = "user.updated.v1";
    public const string UserDeleted = "user.deleted.v1";
    public const string UserDisableOtpRequested = "user.disable-otp.requested.v1";
    public const string UserChangeRoleOtpRequested = "user.change-role-otp.requested.v1";
}
