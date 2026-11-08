using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TeamOrganization.Application.Interfaces.Services.Audit;
using TeamOrganization.Domain.Entities;
using TeamOrganization.Infrastructure.Options;
using TeamOrganization.Infrastructure.Persistence;

namespace TeamOrganization.Infrastructure.Services.Audit;

public sealed class AuditLogBackgroundService : BackgroundService
{
    private readonly Serilog.ILogger _logger;
    private readonly IAuditLogBackgroundQueue _queue;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IOptions<AuditLogOptions> _auditLogOptions;

    public AuditLogBackgroundService(
        Serilog.ILogger logger,
        IAuditLogBackgroundQueue queue,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<AuditLogOptions> auditLogOptions)
    {
        _logger = logger;
        _queue = queue;
        _serviceScopeFactory = serviceScopeFactory;
        _auditLogOptions = auditLogOptions;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _auditLogOptions.Value;

        await foreach (var auditLogs in _queue.ReadAllAsync(stoppingToken))
        {
            for (var retryCount = 0; ; retryCount++)
            {
                try
                {
                    await PersistAsync(auditLogs, stoppingToken);
                    break;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    if (retryCount >= options.RetryCount)
                    {
                        _logger.Error(
                            exception,
                            "Failed to persist {AuditLogCount} audit log entries after {RetryCount} retries; discarding entries",
                            auditLogs.Count,
                            retryCount);

                        break;
                    }

                    var jitter = Random.Shared.Next(options.JitterFromMsSeconds, options.JitterToMsSeconds);

                    var delay = options.RetryDelayInMsSeconds[retryCount] + jitter;

                    _logger.Warning(
                        exception,
                        "Failed to persist {AuditLogCount} audit log entries; retry {RetryAttempt}/{MaxRetryCount} in {DelayMs}ms",
                        auditLogs.Count,
                        retryCount + 1,
                        options.RetryCount,
                        delay);

                    try
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(delay), stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }
        }
    }

    private async Task PersistAsync(IReadOnlyCollection<AuditLog> auditLogs, CancellationToken cancellationToken)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.AuditLogs.AddRange(auditLogs);
        await dbContext.SaveChangesWithoutAuditAsync(cancellationToken);
    }
}
