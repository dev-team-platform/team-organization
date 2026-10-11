namespace TeamOrganization.Domain.Exceptions;

public sealed class NonRetryableMessageException : Exception
{
    public NonRetryableMessageException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
