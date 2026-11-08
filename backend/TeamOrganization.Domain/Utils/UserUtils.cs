using TeamOrganization.Domain.Entities;

namespace TeamOrganization.Domain.Utils;

public static class UserUtils
{
    public static string ConvertDisplayName(User user)
    {
        return user.FirstName.Trim() + " " +
            (string.IsNullOrWhiteSpace(user.LastName) ? "" : user.LastName.Trim());
    }
}