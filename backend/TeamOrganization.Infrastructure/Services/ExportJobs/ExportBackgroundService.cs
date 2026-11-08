using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TeamOrganization.Application.Interfaces.Services.Exports;
using TeamOrganization.Application.Interfaces.Services.FileStorages;
using TeamOrganization.Application.Models.Exports;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Constants.FileStorages;
using TeamOrganization.Domain.Constants.Notifications;
using TeamOrganization.Domain.Entities;
using TeamOrganization.Domain.Enums.FileStorage;
using TeamOrganization.Domain.Exceptions;
using TeamOrganization.Infrastructure.Options;

namespace TeamOrganization.Infrastructure.Services.ExportJobs;

public sealed class ExportBackgroundService : BackgroundService
{
    private readonly IExportQueueService _exportQueueService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IOptions<ExportOptions> _exportOptions;
    private readonly Serilog.ILogger _logger;

    public ExportBackgroundService(
        IExportQueueService exportQueueService,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<ExportOptions> exportOptions,
        Serilog.ILogger logger)
    {
        _exportQueueService = exportQueueService;
        _serviceScopeFactory = serviceScopeFactory;
        _exportOptions = exportOptions;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _exportQueueService.ReadAllAsync(stoppingToken))
        {
            try
            {
                if (!await job.Ready.Task.WaitAsync(stoppingToken))
                {
                    continue;
                }
                await ProcessAsync(job, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Export job {JobId} could not be processed", job.JobId);
            }
        }
    }

    internal async Task ProcessAsync(ExportJobModel job, CancellationToken cancellationToken)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var exportNotificationService = sp.GetRequiredService<IExportNotificationService>();
        Guid? fileId = null;
        try
        {
            var result = await ProcessExportJobAsync(sp, job, cancellationToken);
            fileId = result.FileId;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Export job {JobId} failed", job.JobId);
        }

        var status = fileId.HasValue
            ? NotificationContents.Export.Status.Successful
            : NotificationContents.Export.Status.Failed;
        var retryCount = _exportOptions.Value.RetryCount;
        for (var attempt = 0; attempt < retryCount; attempt++)
        {
            try
            {
                await exportNotificationService.SendAsync(job, status, fileId, cancellationToken);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception exception)
            {
                _logger.Warning(exception, "Failed to publish export {JobId} status {Status}, attempt {Attempt}", job.JobId, status, attempt);
                if (attempt < retryCount)
                {
                    var jitter = Random.Shared.Next(_exportOptions.Value.JitterFromMsSeconds, _exportOptions.Value.JitterToMsSeconds);
                    var delay = _exportOptions.Value.RetryDelayInMsSeconds[attempt] + jitter;
                    await Task.Delay(TimeSpan.FromMilliseconds(delay), cancellationToken);
                }
            }
        }
        _logger.Error("Could not publish export {JobId} status {Status}", job.JobId, status);
    }

    internal async Task<ExportJobResultModel> ProcessExportJobAsync(
        IServiceProvider sp,
        ExportJobModel job,
        CancellationToken cancellationToken = default)
    {
        switch (job.Data)
        {
            case GetAllUsersResponseModel data:
                _logger.Information("Export User List View is starting.");
                return await ProcessExportUserListViewAsync(sp, job, data, cancellationToken);
            default:
                throw new UnprocessableEntityException("Invalid export object model");
        }
    }

    internal async Task<ExportJobResultModel> ProcessExportUserListViewAsync(
        IServiceProvider sp,
        ExportJobModel job,
        GetAllUsersResponseModel data,
        CancellationToken cancellationToken = default)
    {
        var generator = sp.GetRequiredService<IGenerateReportsService<GetAllUsersResponseModel>>();
        var fileStorageService = sp.GetRequiredService<IFileStorageCommandService>();

        await using var stream = await generator.GenerateAsync(data, job.TimeZoneId, cancellationToken);
        var file = new FileStorage
        {
            Id = Guid.CreateVersion7(),
            CreatedById = job.UserId,
            ReferenceId = job.JobId.ToString(),
            ObjectKey = $"exports/users/{job.UserId}/{job.FileName}",
            FileName = job.FileName,
            FileType = FileType.UserListView,
            ContentType = FileContentType.ExcelX,
            FileSize = stream.Length,
            ExpiredAt = job.ExpiredAt
        };

        await fileStorageService.CreateFileStorageAsync(stream, file, cancellationToken);
        return new ExportJobResultModel
        {
            FileId = file.Id
        };
    }
}
