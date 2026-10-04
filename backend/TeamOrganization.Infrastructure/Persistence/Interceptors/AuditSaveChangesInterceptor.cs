using System.Diagnostics;
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TeamOrganization.Domain.Entities;
using TeamOrganization.Domain.Enums;
using TeamOrganization.Application.Interfaces.Services.Audit;

namespace TeamOrganization.Infrastructure.Persistence.Interceptors;

public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditLogBackgroundQueue _auditLogBackgroundQueue;
    private readonly ConcurrentDictionary<Guid, IReadOnlyCollection<AuditLog>> _pendingAuditLogs = new();

    public AuditSaveChangesInterceptor(
        IHttpContextAccessor httpContextAccessor,
        IAuditLogBackgroundQueue auditLogBackgroundQueue)
    {
        _httpContextAccessor = httpContextAccessor;
        _auditLogBackgroundQueue = auditLogBackgroundQueue;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not AppDbContext context)
            return base.SavingChangesAsync(
                eventData,
                result,
                cancellationToken);

        var auditLogs = ApplyAudit(context);
        if (auditLogs.Count > 0)
        {
            _pendingAuditLogs[context.ContextId.InstanceId] = auditLogs;
        }
        else
        {
            _pendingAuditLogs.TryRemove(context.ContextId.InstanceId, out _);
        }

        return base.SavingChangesAsync(
            eventData,
            result,
            cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var savedResult = await base.SavedChangesAsync(eventData, result, cancellationToken);

        if (eventData.Context is AppDbContext context
            && _pendingAuditLogs.TryRemove(context.ContextId.InstanceId, out var auditLogs))
        {
            _auditLogBackgroundQueue.Enqueue(auditLogs);
        }

        return savedResult;
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is AppDbContext context)
        {
            _pendingAuditLogs.TryRemove(context.ContextId.InstanceId, out _);
        }

        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    public override Task SaveChangesCanceledAsync(
        DbContextEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is AppDbContext context)
        {
            _pendingAuditLogs.TryRemove(context.ContextId.InstanceId, out _);
        }

        return base.SaveChangesCanceledAsync(eventData, cancellationToken);
    }

    private IReadOnlyCollection<AuditLog> ApplyAudit(AppDbContext context)
    {
        var actorId = context.CurrentActorId;
        var parsedActorId = Guid.TryParse(actorId, out var guid) ? guid : Guid.Empty;
        var now = DateTimeOffset.UtcNow;
        var auditEntries = new List<AuditLog>();
        var httpContext = _httpContextAccessor.HttpContext;

        foreach (var entry in context.ChangeTracker.Entries<EntityBase>())
        {
            var state = entry.State;

            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedById = parsedActorId == Guid.Empty ? null : parsedActorId;

                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedById = parsedActorId == Guid.Empty ? null : parsedActorId;

                    entry.Property(x => x.CreatedAt).IsModified = false;
                    entry.Property(x => x.CreatedById).IsModified = false;

                    break;
            }

            if (!string.IsNullOrWhiteSpace(actorId)
                && state is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                auditEntries.Add(CreateAuditLog(entry, actorId, now, httpContext));
            }
        }

        return auditEntries;
    }

    private static AuditLog CreateAuditLog(
        EntityEntry<EntityBase> entry,
        string actorId,
        DateTimeOffset now,
        HttpContext? httpContext)
    {
        var action = entry.State switch
        {
            EntityState.Added => AuditAction.Create,
            EntityState.Deleted => AuditAction.Delete,
            EntityState.Modified when entry.Entity.IsDeleted => AuditAction.Delete,
            _ => AuditAction.Update
        };

        return new AuditLog
        {
            Id = Guid.CreateVersion7(),
            Action = action,
            TraceId = Activity.Current?.TraceId.ToString(),
            SpanId = Activity.Current?.SpanId.ToString(),
            RequestId = httpContext?.TraceIdentifier,
            ClientActionId = GetHeader(httpContext, "X-Client-Action-Id"),
            ClientRequestId = GetHeader(httpContext, "X-Client-Request-Id"),
            EntityName = entry.Metadata.ClrType.Name,
            EntityId = entry.Property(nameof(EntityBase.Id)).CurrentValue?.ToString() ?? string.Empty,
            OldValues = action is AuditAction.Update or AuditAction.Delete
                ? SerializeValues(entry, useOriginalValues: true)
                : null,
            NewValues = action is AuditAction.Create or AuditAction.Update
                ? SerializeValues(entry, useOriginalValues: false)
                : null,
            ActorId = actorId,
            CreatedAt = now
        };
    }

    private static string? SerializeValues(EntityEntry<EntityBase> entry, bool useOriginalValues)
    {
        var values = entry.Properties
            .Where(property => property.Metadata.Name is not nameof(EntityBase.CreatedAt)
                and not nameof(EntityBase.CreatedById)
                and not nameof(EntityBase.UpdatedAt)
                and not nameof(EntityBase.UpdatedById))
            .Where(property => entry.State != EntityState.Modified
                || entry.Entity.IsDeleted
                || property.IsModified)
            .ToDictionary(
                property => property.Metadata.Name,
                property => useOriginalValues ? property.OriginalValue : property.CurrentValue);

        return values.Count == 0 ? null : JsonSerializer.Serialize(values);
    }

    private static string? GetHeader(HttpContext? httpContext, string name)
    {
        return httpContext?.Request.Headers[name].FirstOrDefault();
    }
}
