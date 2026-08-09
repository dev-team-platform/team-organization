namespace TeamOrganization.Domain.Exceptions;

public class NotFoundException : Exception
{
    public Dictionary<string, object>? Details { get; set; }

    public NotFoundException(string message, Dictionary<string, object>? details = null) : 
        base(message)
    {
        Details = details;
    }
}