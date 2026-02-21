using FluentAssertions;
using WorkScholarship.Application.Features.Auth.Commands.LoginWithGoogle;

namespace WorkScholarship.Application.Tests.Features.Auth.Commands;

[Trait("Category", "Application")]
[Trait("Feature", "Auth")]
[Trait("Component", "LoginWithGoogleCommandValidator")]
public class LoginWithGoogleCommandValidatorTests
{
    private readonly LoginWithGoogleCommandValidator _validator;

    public LoginWithGoogleCommandValidatorTests()
    {
        _validator = new LoginWithGoogleCommandValidator();
    }

    // =====================================================================
    // Valid inputs
    // =====================================================================

    [Fact]
    public void Validate_WithValidCodeAndRedirectUri_ShouldHaveNoErrors()
    {
        // Arrange
        var command = new LoginWithGoogleCommand("auth-code-123", "https://localhost:7001/api/auth/google/callback");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    // =====================================================================
    // Code validation
    // =====================================================================

    [Fact]
    public void Validate_WithEmptyCode_ShouldHaveCodeError()
    {
        // Arrange
        var command = new LoginWithGoogleCommand("", "https://localhost:7001/api/auth/google/callback");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public void Validate_WithNullCode_ShouldHaveCodeError()
    {
        // Arrange
        var command = new LoginWithGoogleCommand(null!, "https://localhost:7001/api/auth/google/callback");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    // =====================================================================
    // RedirectUri validation
    // =====================================================================

    [Fact]
    public void Validate_WithEmptyRedirectUri_ShouldHaveRedirectUriError()
    {
        // Arrange
        var command = new LoginWithGoogleCommand("auth-code-123", "");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RedirectUri");
    }

    [Fact]
    public void Validate_WithNullRedirectUri_ShouldHaveRedirectUriError()
    {
        // Arrange
        var command = new LoginWithGoogleCommand("auth-code-123", null!);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RedirectUri");
    }

    [Fact]
    public void Validate_WithBothEmpty_ShouldHaveTwoErrors()
    {
        // Arrange
        var command = new LoginWithGoogleCommand("", "");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }
}
