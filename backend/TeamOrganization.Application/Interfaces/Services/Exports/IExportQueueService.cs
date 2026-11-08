using TeamOrganization.Application.Models.Exports;

namespace TeamOrganization.Application.Interfaces.Services.Exports;

public interface IExportQueueService
{
    bool TryEnqueue(ExportJobModel job);
    IAsyncEnumerable<ExportJobModel> ReadAllAsync(CancellationToken cancellationToken);
}
