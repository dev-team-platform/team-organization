namespace TeamOrganization.Application.Interfaces.Services.Exports;

public interface IGenerateReportsService<T> where T : class
{
    Task<Stream> GenerateAsync(T data, TimeZoneInfo timeZoneId, CancellationToken cancellationToken = default);
}
