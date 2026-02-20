using FluentAssertions;
using WorkScholarship.Application.Features.Auth.Commands.Login;

namespace WorkScholarship.Application.Tests.Features.Auth.Commands;

[Trait("Category", "Application")]
[Trait("Feature", "Auth")]
[Trait("Component", "LoginCommandValidator")]
public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    // =====================================================================
    // Email validation
    // =====================================================================

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_WithNullOrEmptyEmail_ReturnsValidationError(string? email)
    {
        // Arrange
        var command = new LoginCommand(email!, "password123");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email" && e.ErrorMessage.Contains("requerido"));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    [InlineData("@nodomain.com")]
    [InlineData("plaintext")]
    public async Task Validate_WithInvalidEmailFormat_ReturnsValidationError(string invalidEmail)
    {
        // Arrange
        var command = new LoginCommand(invalidEmail, "password123");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("user@domain.com")]
    [InlineData("juan.perez@universidad.edu")]
    [InlineData("test+tag@example.org")]
    public async Task Validate_WithValidEmail_DoesNotReturnEmailError(string validEmail)
    {
        // Arrange
        var command = new LoginCommand(validEmail, "password123");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.Errors.Should().NotContain(e => e.PropertyName == "Email");
    }

    // =====================================================================
    // Password validation
    // =====================================================================

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_WithNullOrEmptyPassword_ReturnsValidationError(string? password)
    {
        // Arrange
        var command = new LoginCommand("valid@email.com", password!);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Theory]
    [InlineData("password123")]
    [InlineData("P")]
    [InlineData("any value")]
    public async Task Validate_WithNonEmptyPassword_DoesNotReturnPasswordError(string password)
    {
        // Arrange
        var command = new LoginCommand("valid@email.com", password);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.Errors.Should().NotContain(e => e.PropertyName == "Password");
    }

    // =====================================================================
    // Full valid command
    // =====================================================================

    [Fact]
    public async Task Validate_WithValidEmailAndPassword_ReturnsValid()
    {
        // Arrange
        var command = new LoginCommand("valid@email.com", "password123");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
