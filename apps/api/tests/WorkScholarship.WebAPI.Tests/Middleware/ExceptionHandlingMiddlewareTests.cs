using System.Net;
using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.WebAPI.Middleware;

namespace WorkScholarship.WebAPI.Tests.Middleware;

[Trait("Category", "WebAPI")]
[Trait("Component", "ExceptionHandlingMiddleware")]
public class ExceptionHandlingMiddlewareTests
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddlewareTests()
    {
        _logger = Substitute.For<ILogger<ExceptionHandlingMiddleware>>();
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }

    // =====================================================================
    // Happy path: no exception
    // =====================================================================

    [Fact]
    public async Task InvokeAsync_WithNoException_CallsNext()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithNoException_DoesNotChangeStatusCode()
    {
        // Arrange
        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert - default status code should remain 200
        context.Response.StatusCode.Should().Be(200);
    }

    // =====================================================================
    // ValidationException handling
    // =====================================================================

    [Fact]
    public async Task InvokeAsync_WithValidationException_Returns400()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("Email", "Email is required"),
            new("Password", "Password is required")
        };
        RequestDelegate next = (ctx) => throw new ValidationException(failures);
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task InvokeAsync_WithValidationException_SetsContentTypeToJson()
    {
        // Arrange
        var failures = new List<ValidationFailure> { new("Field", "Error") };
        RequestDelegate next = (ctx) => throw new ValidationException(failures);
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.ContentType.Should().StartWith("application/json");
    }

    [Fact]
    public async Task InvokeAsync_WithValidationException_ReturnsValidationErrorCode()
    {
        // Arrange
        var failures = new List<ValidationFailure> { new("Field", "Error message") };
        RequestDelegate next = (ctx) => throw new ValidationException(failures);
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var body = await ReadResponseBodyAsync(context);
        body.Should().Contain(AuthErrorCodes.VALIDATION_ERROR);
    }

    [Fact]
    public async Task InvokeAsync_WithValidationException_ReturnsValidationDetails()
    {
        // Arrange
        var failures = new List<ValidationFailure> { new("Email", "Email is required") };
        RequestDelegate next = (ctx) => throw new ValidationException(failures);
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var body = await ReadResponseBodyAsync(context);
        body.Should().Contain("Email");
        body.Should().Contain("Email is required");
    }

    [Fact]
    public async Task InvokeAsync_WithValidationException_LogsWarning()
    {
        // Arrange
        var failures = new List<ValidationFailure> { new("Field", "Error") };
        RequestDelegate next = (ctx) => throw new ValidationException(failures);
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert - verify logger was called with Warning level
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<ValidationException>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // =====================================================================
    // UnauthorizedAccessException handling
    // =====================================================================

    [Fact]
    public async Task InvokeAsync_WithUnauthorizedAccessException_Returns401()
    {
        // Arrange
        RequestDelegate next = (ctx) => throw new UnauthorizedAccessException("Access denied");
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InvokeAsync_WithUnauthorizedAccessException_SetsContentTypeToJson()
    {
        // Arrange
        RequestDelegate next = (ctx) => throw new UnauthorizedAccessException();
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.ContentType.Should().StartWith("application/json");
    }

    [Fact]
    public async Task InvokeAsync_WithUnauthorizedAccessException_ReturnsUnauthorizedCode()
    {
        // Arrange
        RequestDelegate next = (ctx) => throw new UnauthorizedAccessException();
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var body = await ReadResponseBodyAsync(context);
        body.Should().Contain(AuthErrorCodes.UNAUTHORIZED);
    }

    [Fact]
    public async Task InvokeAsync_WithUnauthorizedAccessException_LogsWarning()
    {
        // Arrange
        RequestDelegate next = (ctx) => throw new UnauthorizedAccessException();
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<UnauthorizedAccessException>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // =====================================================================
    // Generic exception handling
    // =====================================================================

    [Fact]
    public async Task InvokeAsync_WithGenericException_Returns500()
    {
        // Arrange
        RequestDelegate next = (ctx) => throw new InvalidOperationException("Something went wrong");
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task InvokeAsync_WithGenericException_SetsContentTypeToJson()
    {
        // Arrange
        RequestDelegate next = (ctx) => throw new Exception("Unexpected error");
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.ContentType.Should().StartWith("application/json");
    }

    [Fact]
    public async Task InvokeAsync_WithGenericException_ReturnsInternalServerErrorCode()
    {
        // Arrange
        RequestDelegate next = (ctx) => throw new Exception("Unexpected error");
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var body = await ReadResponseBodyAsync(context);
        body.Should().Contain("INTERNAL_SERVER_ERROR");
    }

    [Fact]
    public async Task InvokeAsync_WithGenericException_DoesNotExposeExceptionDetails()
    {
        // Arrange - the internal exception message should NOT be in the response
        var secretMessage = "Sensitive database connection string error";
        RequestDelegate next = (ctx) => throw new Exception(secretMessage);
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert - sensitive info not exposed
        var body = await ReadResponseBodyAsync(context);
        body.Should().NotContain(secretMessage);
    }

    [Fact]
    public async Task InvokeAsync_WithGenericException_LogsError()
    {
        // Arrange
        RequestDelegate next = (ctx) => throw new Exception("Some exception");
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // =====================================================================
    // Response format validation
    // =====================================================================

    [Fact]
    public async Task InvokeAsync_WithValidationException_ResponseIsValidJson()
    {
        // Arrange
        var failures = new List<ValidationFailure> { new("Field", "Error") };
        RequestDelegate next = (ctx) => throw new ValidationException(failures);
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var body = await ReadResponseBodyAsync(context);
        var act = () => JsonDocument.Parse(body);
        act.Should().NotThrow("Response should be valid JSON");
    }

    [Fact]
    public async Task InvokeAsync_WithGenericException_ResponseIsValidJson()
    {
        // Arrange
        RequestDelegate next = (ctx) => throw new Exception("Error");
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var body = await ReadResponseBodyAsync(context);
        var act = () => JsonDocument.Parse(body);
        act.Should().NotThrow("Response should be valid JSON");
    }

    [Fact]
    public async Task InvokeAsync_WithUnauthorizedException_ResponseIsValidJson()
    {
        // Arrange
        RequestDelegate next = (ctx) => throw new UnauthorizedAccessException();
        var middleware = new ExceptionHandlingMiddleware(next, _logger);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var body = await ReadResponseBodyAsync(context);
        var act = () => JsonDocument.Parse(body);
        act.Should().NotThrow("Response should be valid JSON");
    }
}
