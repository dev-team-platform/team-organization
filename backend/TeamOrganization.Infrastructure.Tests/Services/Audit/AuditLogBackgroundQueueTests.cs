using AwesomeAssertions;
using TeamOrganization.Domain.Entities;
using TeamOrganization.Infrastructure.Services.Audit;
using Xunit;

namespace TeamOrganization.Infrastructure.Tests.Services.Audit;

public class AuditLogBackgroundQueueTests
{
    [Fact]
    public async Task Enqueue_ShouldMakeBatchAvailableToReader_WhenBatchIsNonempty()
    {
        var queue = new AuditLogBackgroundQueue();
        IReadOnlyCollection<AuditLog> batch = [new() { Id = Guid.NewGuid(), EntityName = "User" }];

        queue.Enqueue(batch);

        await using var reader = queue.ReadAllAsync(CancellationToken.None).GetAsyncEnumerator();
        (await reader.MoveNextAsync()).Should().BeTrue();
        reader.Current.Should().BeSameAs(batch);
    }

    [Fact]
    public async Task Enqueue_ShouldIgnoreBatch_WhenBatchIsEmpty()
    {
        var queue = new AuditLogBackgroundQueue();
        IReadOnlyCollection<AuditLog> empty = [];
        IReadOnlyCollection<AuditLog> expected = [new() { EntityName = "User" }];
        queue.Enqueue(empty);
        queue.Enqueue(expected);

        await using var reader = queue.ReadAllAsync(CancellationToken.None).GetAsyncEnumerator();
        (await reader.MoveNextAsync()).Should().BeTrue();
        reader.Current.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task ReadAllAsync_ShouldStopReading_WhenCancellationIsRequested()
    {
        var queue = new AuditLogBackgroundQueue();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await using var reader = queue.ReadAllAsync(cancellation.Token).GetAsyncEnumerator();

        Func<Task> act = async () => await reader.MoveNextAsync();
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
