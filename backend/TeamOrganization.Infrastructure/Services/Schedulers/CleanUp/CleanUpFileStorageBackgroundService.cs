using Microsoft.Extensions.Hosting;

namespace TeamOrganization.Infrastructure.Services.Schedulers.CleanUp;

public class CleanUpFileStorageScheduler : BackgroundService
{
    public CleanUpFileStorageScheduler()
    {
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
    }
}