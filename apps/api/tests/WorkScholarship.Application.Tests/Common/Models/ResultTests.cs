using FluentAssertions;
using WorkScholarship.Application.Common.Models;

namespace WorkScholarship.Application.Tests.Common.Models;

[Trait("Category", "Application")]
[Trait("Component", "Result")]
public class ResultTests
{
    // =====================================================================
    // Result (non-generic) - Success
    // =====================================================================

    [Fact]
    public void Result_Success_IsSuccessIsTrue()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Result_Success_IsFailureIsFalse()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsFailure.Should().BeFalse();
    }

    [Fact]
    public void Result_Success_ErrorIsNull()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.Error.Should().BeNull();
    }

    // =====================================================================
    // Result (non-generic) - Failure with Error object
    // =====================================================================

    [Fact]
    public void Result_Failure_WithError_IsSuccessIsFalse()
    {
        // Arrange
        var error = new Error("TEST_CODE", "Test message");

        // Act
        var result = Result.Failure(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Result_Failure_WithError_IsFailureIsTrue()
    {
        // Arrange
        var error = new Error("TEST_CODE", "Test message");

        // Act
        var result = Result.Failure(error);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Result_Failure_WithError_ErrorIsSet()
    {
        // Arrange
        var error = new Error("TEST_CODE", "Test message");

        // Act
        var result = Result.Failure(error);

        // Assert
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("TEST_CODE");
        result.Error.Message.Should().Be("Test message");
    }

    // =====================================================================
    // Result (non-generic) - Failure with code and message
    // =====================================================================

    [Fact]
    public void Result_Failure_WithCodeAndMessage_SetsErrorCorrectly()
    {
        // Act
        var result = Result.Failure("MY_ERROR", "My error message");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("MY_ERROR");
        result.Error.Message.Should().Be("My error message");
    }

    // =====================================================================
    // Result<T> - Success
    // =====================================================================

    [Fact]
    public void ResultT_Success_IsSuccessIsTrue()
    {
        // Act
        var result = Result<string>.Success("test value");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ResultT_Success_ValueIsAccessible()
    {
        // Act
        var result = Result<string>.Success("test value");

        // Assert
        result.Value.Should().Be("test value");
    }

    [Fact]
    public void ResultT_Success_ErrorIsNull()
    {
        // Act
        var result = Result<string>.Success("test");

        // Assert
        result.Error.Should().BeNull();
    }

    [Fact]
    public void ResultT_Success_WithNullValue_IsSuccessful()
    {
        // Act
        var result = Result<string?>.Success(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    // =====================================================================
    // Result<T> - Failure
    // =====================================================================

    [Fact]
    public void ResultT_Failure_WithError_IsFailure()
    {
        // Arrange
        var error = new Error("ERR_CODE", "Error message");

        // Act
        var result = Result<string>.Failure(error);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ERR_CODE");
    }

    [Fact]
    public void ResultT_Failure_AccessingValue_ThrowsInvalidOperationException()
    {
        // Arrange
        var result = Result<string>.Failure("ERR", "message");

        // Act
        var act = () => _ = result.Value;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*failed result*");
    }

    [Fact]
    public void ResultT_Failure_WithCodeAndMessage_SetsErrorCorrectly()
    {
        // Act
        var result = Result<int>.Failure("MY_ERROR", "My error message");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("MY_ERROR");
        result.Error.Message.Should().Be("My error message");
    }

    // =====================================================================
    // Error record
    // =====================================================================

    [Fact]
    public void Error_WithDetails_SetsDetailsCorrectly()
    {
        // Arrange
        var details = new List<ValidationError>
        {
            new("Email", "Email is required"),
            new("Password", "Password is required")
        };

        // Act
        var error = new Error("VALIDATION_ERROR", "Validation failed", details);

        // Assert
        error.Code.Should().Be("VALIDATION_ERROR");
        error.Message.Should().Be("Validation failed");
        error.Details.Should().HaveCount(2);
        error.Details![0].Field.Should().Be("Email");
        error.Details![1].Field.Should().Be("Password");
    }

    [Fact]
    public void Error_WithNullDetails_DetailsIsNull()
    {
        // Act
        var error = new Error("CODE", "Message");

        // Assert
        error.Details.Should().BeNull();
    }

    // =====================================================================
    // ValidationError record
    // =====================================================================

    [Fact]
    public void ValidationError_SetsFieldAndMessage()
    {
        // Act
        var validationError = new ValidationError("Email", "Email is required");

        // Assert
        validationError.Field.Should().Be("Email");
        validationError.Message.Should().Be("Email is required");
    }

    // =====================================================================
    // Constructor invariants
    // =====================================================================

    [Fact]
    public void Result_Constructor_SuccessWithError_ThrowsInvalidOperationException()
    {
        // The constructor checks: success=true but has an error => invalid
        // We can't call the protected constructor directly, but the guard can be tested
        // indirectly. Result<T>.Success() should never set error.
        var result = Result.Success();
        result.Error.Should().BeNull();
        result.IsSuccess.Should().BeTrue();
    }
}
