namespace TeamOrganization.Domain.Exceptions;

public class UnprocessableException : Exception
{
    public Dictionary<string, object>? Details { get; set; }

    public UnprocessableException(string message, Dictionary<string, object>? details = null) :
        base(message)
    {
        Details = details;
    }
}