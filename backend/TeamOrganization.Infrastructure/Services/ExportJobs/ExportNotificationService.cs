using System.Text.Json;
using TeamOrganization.Application.Interfaces.Services.Exports;
using TeamOrganization.Application.Interfaces.Services.Messaging;
using TeamOrganization.Application.Models.Exports;
using TeamOrganization.Application.Models.Messaging.Publishing;
using TeamOrganization.Application.Models.Messaging.Publishing.Notifications;
using TeamOrganization.Domain.Constants.EventRoutingKeys;
using TeamOrganization.Domain.Constants.Notifications;
using TeamOrganization.Domain.Utils;

namespace TeamOrganization.Infrastructure.Services.ExportJobs;

public sealed class ExportNotificationService : IExportNotificationService
{
    private readonly IMessagingPublisher<OutboxEvent<SendNotificationEventModel>> _messagingPublisher;

    public ExportNotificationService(IMessagingPublisher<OutboxEvent<SendNotificationEventModel>> messagingPublisher)
    {
        _messagingPublisher = messagingPublisher;
    }

    public Task SendAsync(ExportJobModel job, string status, Guid? fileId = null, CancellationToken cancellationToken = default)
    {
        return _messagingPublisher.HandleAsync(new OutboxEvent<SendNotificationEventModel>
        {
            Type = EventRoutingKeys.UserExported,
            RoutingKey = EventRoutingKeys.UserExported,
            Data = CreateEvent(job, status, fileId),
            ThrowOnFailure = true
        }, cancellationToken);
    }

    internal static SendNotificationEventModel CreateEvent(ExportJobModel job, string status, Guid? fileId)
    {
        (string Title, string Content) contents = status switch
        {
            NotificationContents.Export.Status.InProgress
                => (NotificationContents.Export.Title.InProgress,
                    NotificationContents.Export.Content.InProgress),
            NotificationContents.Export.Status.Successful
                => (NotificationContents.Export.Title.Successful,
                    NotificationContents.Export.Content.Successful),
            NotificationContents.Export.Status.Failed
                => (NotificationContents.Export.Title.Failed,
                    NotificationContents.Export.Content.Failed),
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };
        return new SendNotificationEventModel
        {
            EventType = EventRoutingKeys.UserExported,
            CreatedById = job.UserId,
            UpdatedById = status == NotificationContents.Export.Status.InProgress ? null : job.UserId,
            Notification = new NotificationEventModel
            {
                NotificationId = job.NotificationId,
                Title = contents.Title,
                Content = contents.Content,
                Recipients = [new NotificationRecipientEventModel { UserId = job.UserId, IdentitySubject = job.IdentitySubject }],
                Data = JsonSerializer.SerializeToDocument(new { status, fileName = job.FileName, fileStorageId = fileId }, JsonUtils.WebSerializerOptions)
            }
        };
    }
}
