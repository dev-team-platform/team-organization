namespace TeamOrganization.Domain.Exceptions;

public class UnauthorizedException : Exception
{
    public Dictionary<string, object>? Details { get; set; }

    public UnauthorizedException(string message, Dictionary<string, object>? details = null) :
        base(message)
    {
        Details = details;
    }
}