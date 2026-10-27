namespace TeamOrganization.Domain.Constants.CacheKeys;

public static class UserCacheKeys
{
    private const string GetCurrentUser = "team-organization:users:current-user:{sub}";
    private const string DisableOtp = "team-organization:users:disable-otp:{userId}";
    private const string DisableOtpWrongTimes = "team-organization:users:disable-otp-wrong-times:{userId}";
    private const string DisableOtpLock = "team-organization:users:disable-otp-lock:{userId}";
    private const string ChangeRoleOtp = "team-organization:users:change-role-otp:{userId}";
    private const string ChangeRoleOtpWrongTimes = "team-organization:users:change-role-otp-wrong-times:{userId}";
    private const string ChangeRoleOtpLock = "team-organization:users:change-role-otp-lock:{userId}";

    public const int OtpExpirationMinutes = 5;

    private static readonly TimeSpan OtpExpiration = TimeSpan.FromMinutes(OtpExpirationMinutes);
    private static readonly TimeSpan WrongTimesExpiration = TimeSpan.FromHours(24);

    public static CacheKeyObject GetCurrentUserKey(string sub) => new()
    {
        Key = GetCurrentUser.Replace("{sub}", sub),
        Expiration = CacheKeyObject.GetJitter(TimeSpan.FromMinutes(30), 300)
    };

    public static CacheKeyObject GetDisableOtpKey(Guid userId) => new()
    {
        Key = DisableOtp.Replace("{userId}", userId.ToString()),
        Expiration = OtpExpiration
    };

    public static CacheKeyObject GetDisableOtpWrongTimesKey(Guid userId) => new()
    {
        Key = DisableOtpWrongTimes.Replace("{userId}", userId.ToString()),
        Expiration = WrongTimesExpiration
    };

    public static CacheKeyObject GetDisableOtpLockKey(Guid userId) => new()
    {
        Key = DisableOtpLock.Replace("{userId}", userId.ToString()),
        Expiration = GetLockExpiration()
    };

    public static CacheKeyObject GetChangeRoleOtpKey(Guid userId) => new()
    {
        Key = ChangeRoleOtp.Replace("{userId}", userId.ToString()),
        Expiration = OtpExpiration
    };

    public static CacheKeyObject GetChangeRoleOtpWrongTimesKey(Guid userId) => new()
    {
        Key = ChangeRoleOtpWrongTimes.Replace("{userId}", userId.ToString()),
        Expiration = WrongTimesExpiration
    };

    public static CacheKeyObject GetChangeRoleOtpLockKey(Guid userId) => new()
    {
        Key = ChangeRoleOtpLock.Replace("{userId}", userId.ToString()),
        Expiration = GetLockExpiration()
    };

    private static TimeSpan GetLockExpiration() => CacheKeyObject.GetJitter(TimeSpan.FromHours(1), 900);
}
