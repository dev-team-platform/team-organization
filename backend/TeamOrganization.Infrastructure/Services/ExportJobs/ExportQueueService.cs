using System.Threading.Channels;
using Microsoft.Extensions.Options;
using TeamOrganization.Application.Interfaces.Services.Exports;
using TeamOrganization.Application.Models.Exports;
using TeamOrganization.Infrastructure.Options;

namespace TeamOrganization.Infrastructure.Services.ExportJobs;

public sealed class ExportQueueService : IExportQueueService
{
    private readonly Channel<ExportJobModel> _queue;
    private readonly IOptions<ExportOptions> _exportOptions;

    public ExportQueueService(IOptions<ExportOptions> exportOptions)
    {
        _exportOptions = exportOptions;
        _queue = Channel.CreateBounded<ExportJobModel>(new BoundedChannelOptions(_exportOptions.Value.MaxQueueItems)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
        });
    }

    public bool TryEnqueue(ExportJobModel job)
        => _queue.Writer.TryWrite(job);

    public IAsyncEnumerable<ExportJobModel> ReadAllAsync(CancellationToken cancellationToken)
        => _queue.Reader.ReadAllAsync(cancellationToken);
}
