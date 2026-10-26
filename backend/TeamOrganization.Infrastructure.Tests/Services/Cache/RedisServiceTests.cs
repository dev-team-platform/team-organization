using AwesomeAssertions;
using Moq;
using StackExchange.Redis;
using TeamOrganization.Infrastructure.Services.Cache;
using Xunit;

namespace TeamOrganization.Infrastructure.Tests.Services.Cache;

public class RedisServiceTests
{
    private readonly Mock<IDatabase> _database = new();
    private readonly Mock<IConnectionMultiplexer> _connection = new();

    public RedisServiceTests()
    {
        _connection.Setup(connection => connection.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_database.Object);
    }

    [Fact]
    public async Task GetAsync_ShouldDeserializeCachedJson_WhenKeyExists()
    {
        _database.Setup(database => database.StringGetAsync("users:42", It.IsAny<CommandFlags>()))
            .ReturnsAsync("{\"name\":\"Alice\"}");
        var service = new RedisService(_connection.Object);

        var result = await service.GetAsync<CachedUser>("users:42");

        result!.Name.Should().Be("Alice");
    }

    [Fact]
    public async Task GetAsync_ShouldReturnDefault_WhenKeyIsMissing()
    {
        _database.Setup(database => database.StringGetAsync("users:missing", It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var result = await new RedisService(_connection.Object).GetAsync<CachedUser>("users:missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_ShouldThrowArgumentException_WhenKeyIsBlank()
    {
        Func<Task> act = () => new RedisService(_connection.Object).GetAsync<CachedUser>(" ");
        await act.Should().ThrowAsync<ArgumentException>();

        _database.Verify(database => database.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task SetAsync_ShouldSerializeValueAndUseExpiration_WhenValueIsValid()
    {
        var expiry = TimeSpan.FromMinutes(5);
        var actualExpiration = Expiration.Default;
        _database.Setup(database => database.StringSetAsync(
                "users:42", It.IsAny<RedisValue>(), It.IsAny<Expiration>(),
                It.IsAny<ValueCondition>(), CommandFlags.None))
            .Callback<RedisKey, RedisValue, Expiration, ValueCondition, CommandFlags>(
                (_, _, expiration, _, _) => actualExpiration = expiration)
            .ReturnsAsync(true);

        await new RedisService(_connection.Object).SetAsync("users:42", new CachedUser { Name = "Alice" }, expiry);

        _database.Verify(database => database.StringSetAsync(
            "users:42",
            It.Is<RedisValue>(value => value.ToString() == "{\"name\":\"Alice\"}"),
            It.IsAny<Expiration>(),
            It.IsAny<ValueCondition>(),
            CommandFlags.None), Times.Once);
        actualExpiration.Should().Be(new Expiration(expiry));
    }

    [Fact]
    public async Task SetAsync_ShouldThrowInvalidOperationException_WhenRedisRejectsWrite()
    {
        _database.Setup(database => database.StringSetAsync(
                "users:42", It.IsAny<RedisValue>(), It.IsAny<Expiration>(),
                It.IsAny<ValueCondition>(), CommandFlags.None))
            .ReturnsAsync(false);

        Func<Task> act = () => new RedisService(_connection.Object).SetAsync(
            "users:42", new CachedUser { Name = "Alice" });
        var exception = (await act.Should().ThrowAsync<InvalidOperationException>()).Which;

        exception.Message.Should().Contain("users:42");
    }

    [Fact]
    public async Task RemoveAsync_ShouldDeleteKey_WhenKeyIsValid()
    {
        _database.Setup(database => database.KeyDeleteAsync("users:42", CommandFlags.None)).ReturnsAsync(true);

        await new RedisService(_connection.Object).RemoveAsync("users:42");

        _database.Verify(database => database.KeyDeleteAsync("users:42", CommandFlags.None), Times.Once);
    }

    private sealed class CachedUser
    {
        public string Name { get; set; } = null!;
    }
}
