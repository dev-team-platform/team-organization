using TeamOrganization.Application.Interfaces.Services.Exports;
using TeamOrganization.Application.Interfaces.Services.Users;
using TeamOrganization.Application.Models.Exports;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Constants.Notifications;
using TeamOrganization.Domain.Exceptions;

namespace TeamOrganization.Application.Services.Exports;

public class ExportService : IExportService
{

    private readonly IUserQueryService _userQueryService;
    private readonly IExportQueueService _exportQueueService;
    private readonly IExportNotificationService _exportNotificationService;

    public ExportService(
        IUserQueryService userQueryService,
        IExportQueueService exportQueueService,
        IExportNotificationService exportNotificationService)
    {
        _userQueryService = userQueryService;
        _exportQueueService = exportQueueService;
        _exportNotificationService = exportNotificationService;
    }

    public async Task<ExportResponseModel> ExportUsersAsync(GetAllUsersRequestModel model, CancellationToken cancellationToken = default)
    {
        var user = await _userQueryService.GetCurrentUserAsync(cancellationToken);
        var data = await _userQueryService.GetAllUsersAsync(model, cancellationToken);
        var utcNow = DateTimeOffset.UtcNow;
        var timestamp = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, model.TimeZoneId);

        var job = new ExportJobModel
        {
            JobId = Guid.CreateVersion7(),
            NotificationId = Guid.CreateVersion7(),
            UserId = user.Id,
            IdentitySubject = user.IdentitySubject,
            FileName = $"UserListView_{timestamp:ddMMyyyy_HHmmss}.xlsx",
            Data = data,
            TimeZoneId = model.TimeZoneId,
            ExpiredAt = utcNow.AddDays(30)
        };

        if (!_exportQueueService.TryEnqueue(job))
        {
            throw new UnprocessableEntityException("The export queue is full. Please try again later.");
        }

        try
        {
            await _exportNotificationService.SendAsync(
                job,
                NotificationContents.Export.Status.InProgress,
                cancellationToken: cancellationToken);

            job.Ready.TrySetResult(true);
        }
        catch
        {
            job.Ready.TrySetResult(false);
            throw;
        }

        return new ExportResponseModel
        {
            FileName = job.FileName
        };
    }
}
