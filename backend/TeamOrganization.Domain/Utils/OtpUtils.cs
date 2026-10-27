using System.Security.Cryptography;

namespace TeamOrganization.Domain.Utils;

public static class OtpUtils
{
    private const string Characters = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public const int OtpLength = 10;

    public static string GenerateOtp()
    {
        Span<char> otp = stackalloc char[OtpLength];

        for (var index = 0; index < otp.Length; index++)
        {
            otp[index] = Characters[RandomNumberGenerator.GetInt32(Characters.Length)];
        }

        return new string(otp);
    }
}
