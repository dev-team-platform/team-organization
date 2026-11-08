using TeamOrganization.Application.Models.Exports;

namespace TeamOrganization.Application.Interfaces.Services.Exports;

public interface IExportNotificationService
{
    Task SendAsync(ExportJobModel job, string status, Guid? fileId = null, CancellationToken cancellationToken = default);
}
