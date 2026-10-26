using AwesomeAssertions;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Moq;
using TeamOrganization.Api.Middlewares;
using TeamOrganization.Domain.Exceptions;
using Xunit;

namespace TeamOrganization.Api.Tests.Middlewares;

public class GlobalExceptionHandlerMiddlewareTests
{
    [Theory]
    [InlineData(typeof(UnauthorizedException), 401)]
    [InlineData(typeof(ForbiddenException), 403)]
    [InlineData(typeof(NotFoundException), 404)]
    [InlineData(typeof(ConflictException), 409)]
    [InlineData(typeof(UnprocessableEntityException), 422)]
    [InlineData(typeof(InvalidOperationException), 500)]
    public async Task InvokeAsync_ShouldMapExceptionToExpectedStatusAndSafeBody_WhenExceptionIsThrown(Type exceptionType, int statusCode)
    {
        var logger = new Mock<Serilog.ILogger>();
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => Task.FromException(CreateException(exceptionType)), logger.Object);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
        context.Response.StatusCode.Should().Be(statusCode);
        context.Response.ContentType.Should().Be("application/json");
        body.TryGetProperty("message", out _).Should().BeTrue();
        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            body.GetProperty("message").GetString().Should().Be("Something went wrong.");
        }
    }

    [Fact]
    public async Task InvokeAsync_ShouldRethrowException_WhenResponseHasAlreadyStarted()
    {
        var context = new DefaultHttpContext();
        var responseFeature = new Mock<IHttpResponseFeature>();
        responseFeature.SetupGet(feature => feature.HasStarted).Returns(true);
        responseFeature.SetupProperty(feature => feature.StatusCode, StatusCodes.Status200OK);
        context.Features.Set<IHttpResponseFeature>(responseFeature.Object);
        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => Task.FromException(new InvalidOperationException("late failure")),
            Mock.Of<Serilog.ILogger>());

        Func<Task> act = () => middleware.InvokeAsync(context);
        await act.Should().ThrowAsync<InvalidOperationException>();

        context.Response.HasStarted.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    private static Exception CreateException(Type type) => type switch
    {
        var value when value == typeof(UnauthorizedException) => new UnauthorizedException("unauthorized"),
        var value when value == typeof(ForbiddenException) => new ForbiddenException("forbidden"),
        var value when value == typeof(NotFoundException) => new NotFoundException("missing"),
        var value when value == typeof(ConflictException) => new ConflictException("conflict"),
        var value when value == typeof(UnprocessableEntityException) => new UnprocessableEntityException("invalid"),
        _ => new InvalidOperationException("secret detail")
    };
}
