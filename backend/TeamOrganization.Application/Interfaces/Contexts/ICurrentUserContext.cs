namespace TeamOrganization.Application.Interfaces.Contexts;

public interface ICurrentUserContext
{
    string IdentitySubject { get; }
}