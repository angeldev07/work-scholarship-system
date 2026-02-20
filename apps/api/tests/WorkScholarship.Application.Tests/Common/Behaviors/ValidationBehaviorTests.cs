using FluentAssertions;
using FluentValidation;
using MediatR;
using NSubstitute;
using WorkScholarship.Application.Common.Behaviors;
using WorkScholarship.Application.Common.Models;

namespace WorkScholarship.Application.Tests.Common.Behaviors;

[Trait("Category", "Application")]
[Trait("Component", "ValidationBehavior")]
public class ValidationBehaviorTests
{
    // =====================================================================
    // Test request/response types for the behavior
    // =====================================================================

    private record TestRequest(string Value) : IRequest<Result<string>>;
    private record TestRequestNoResult(string Value) : IRequest<Result>;
    private record TestRequestNonResult(string Value) : IRequest<string>;

    private class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(x => x.Value)
                .NotEmpty().WithMessage("Value is required.")
                .MinimumLength(3).WithMessage("Value must be at least 3 characters.");
        }
    }

    private class TestRequestNoResultValidator : AbstractValidator<TestRequestNoResult>
    {
        public TestRequestNoResultValidator()
        {
            RuleFor(x => x.Value)
                .NotEmpty().WithMessage("Value is required.");
        }
    }

    private class AlwaysFailValidator : AbstractValidator<TestRequest>
    {
        public AlwaysFailValidator()
        {
            RuleFor(x => x.Value).Must(_ => false).WithMessage("Always fails.");
        }
    }

    // =====================================================================
    // Tests: No validators registered
    // =====================================================================

    [Fact]
    public async Task Handle_WithNoValidators_CallsNextDelegate()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestRequest>>();
        var behavior = new ValidationBehavior<TestRequest, Result<string>>(validators);
        var nextCalled = false;
        RequestHandlerDelegate<Result<string>> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult(Result<string>.Success("ok"));
        };

        // Act
        var result = await behavior.Handle(new TestRequest("test"), next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
    }

    // =====================================================================
    // Tests: Validation passes
    // =====================================================================

    [Fact]
    public async Task Handle_WithValidRequest_CallsNextDelegateAndReturnsSuccess()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>> { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, Result<string>>(validators);
        var nextCalled = false;
        RequestHandlerDelegate<Result<string>> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult(Result<string>.Success("handler result"));
        };

        // Act
        var result = await behavior.Handle(new TestRequest("valid-value"), next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("handler result");
    }

    // =====================================================================
    // Tests: Validation fails - Result<T> response
    // =====================================================================

    [Fact]
    public async Task Handle_WithInvalidRequest_DoesNotCallNextDelegate()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>> { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, Result<string>>(validators);
        var nextCalled = false;
        RequestHandlerDelegate<Result<string>> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult(Result<string>.Success("should not reach"));
        };

        // Act
        await behavior.Handle(new TestRequest(""), next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ReturnsFailureWithValidationErrorCode()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>> { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, Result<string>>(validators);
        RequestHandlerDelegate<Result<string>> next = (ct) =>
            Task.FromResult(Result<string>.Success("ok"));

        // Act
        var result = await behavior.Handle(new TestRequest(""), next, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AuthErrorCodes.VALIDATION_ERROR);
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ReturnsValidationErrorsInDetails()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>> { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, Result<string>>(validators);
        RequestHandlerDelegate<Result<string>> next = (ct) =>
            Task.FromResult(Result<string>.Success("ok"));

        // Act
        var result = await behavior.Handle(new TestRequest(""), next, CancellationToken.None);

        // Assert
        result.Error!.Details.Should().NotBeNull();
        result.Error.Details!.Should().NotBeEmpty();
        result.Error.Details!.Should().Contain(ve => ve.Field == "Value");
    }

    [Fact]
    public async Task Handle_WithMultipleValidationErrors_ReturnsAllErrors()
    {
        // Arrange - empty string fails both NotEmpty and MinimumLength
        var validators = new List<IValidator<TestRequest>> { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, Result<string>>(validators);
        RequestHandlerDelegate<Result<string>> next = (ct) =>
            Task.FromResult(Result<string>.Success("ok"));

        // Act
        var result = await behavior.Handle(new TestRequest(""), next, CancellationToken.None);

        // Assert
        result.Error!.Details!.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task Handle_WithMultipleValidators_RunsAll()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>>
        {
            new TestRequestValidator(),
            new AlwaysFailValidator()
        };
        var behavior = new ValidationBehavior<TestRequest, Result<string>>(validators);
        RequestHandlerDelegate<Result<string>> next = (ct) =>
            Task.FromResult(Result<string>.Success("ok"));

        // Act
        var result = await behavior.Handle(new TestRequest("valid-input"), next, CancellationToken.None);

        // Assert - second validator always fails
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AuthErrorCodes.VALIDATION_ERROR);
    }

    // =====================================================================
    // Tests: Result (non-generic) response
    // =====================================================================

    [Fact]
    public async Task Handle_WithInvalidRequestAndResultResponse_ReturnsFailureResult()
    {
        // Arrange
        var validators = new List<IValidator<TestRequestNoResult>> { new TestRequestNoResultValidator() };
        var behavior = new ValidationBehavior<TestRequestNoResult, Result>(validators);
        RequestHandlerDelegate<Result> next = (ct) => Task.FromResult(Result.Success());

        // Act
        var result = await behavior.Handle(new TestRequestNoResult(""), next, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AuthErrorCodes.VALIDATION_ERROR);
    }

    [Fact]
    public async Task Handle_WithValidRequestAndResultResponse_CallsNext()
    {
        // Arrange
        var validators = new List<IValidator<TestRequestNoResult>> { new TestRequestNoResultValidator() };
        var behavior = new ValidationBehavior<TestRequestNoResult, Result>(validators);
        var nextCalled = false;
        RequestHandlerDelegate<Result> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success());
        };

        // Act
        await behavior.Handle(new TestRequestNoResult("valid"), next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
    }

    // =====================================================================
    // Tests: Non-Result response type (throws ValidationException)
    // =====================================================================

    [Fact]
    public async Task Handle_WithInvalidRequestAndNonResultResponse_ThrowsValidationException()
    {
        // Arrange
        var validator = new InlineValidator<TestRequestNonResult>();
        validator.RuleFor(x => x.Value).NotEmpty();
        var validators = new List<IValidator<TestRequestNonResult>> { validator };
        var behavior = new ValidationBehavior<TestRequestNonResult, string>(validators);
        RequestHandlerDelegate<string> next = (ct) => Task.FromResult("ok");

        // Act
        var act = async () => await behavior.Handle(new TestRequestNonResult(""), next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }
}
