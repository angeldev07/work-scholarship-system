using FluentAssertions;
using WorkScholarship.Application.Features.Auth.Commands.ForgotPassword;

namespace WorkScholarship.Application.Tests.Features.Auth.Commands;

/// <summary>
/// Tests unitarios para ForgotPasswordCommandValidator.
/// Verifica que el validador rechace emails vacíos o con formato inválido
/// y acepte emails correctamente formateados.
/// </summary>
[Trait("Category", "Application")]
[Trait("Feature", "Auth")]
[Trait("Component", "ForgotPasswordCommandValidator")]
public class ForgotPasswordCommandValidatorTests
{
    private readonly ForgotPasswordCommandValidator _validator = new();

    // =====================================================================
    // Email — campo requerido
    // =====================================================================

    /// <summary>
    /// Verifica que el validador retorne error cuando el email es nulo o cadena vacía.
    /// El mensaje de error debe indicar que el campo es requerido.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_WithNullOrEmptyEmail_ReturnsValidationError(string? email)
    {
        // Arrange
        var command = new ForgotPasswordCommand(email!);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Email" && e.ErrorMessage.Contains("requerido"));
    }

    // =====================================================================
    // Email — formato inválido
    // =====================================================================

    /// <summary>
    /// Verifica que el validador rechace strings que no tienen formato de email válido.
    /// FluentValidation usa la regla EmailAddress() de RFC 5321.
    /// </summary>
    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    [InlineData("@nodomain.com")]
    [InlineData("plaintext")]
    public async Task Validate_WithInvalidEmailFormat_ReturnsValidationError(string invalidEmail)
    {
        // Arrange
        var command = new ForgotPasswordCommand(invalidEmail);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    // =====================================================================
    // Email — formato válido
    // =====================================================================

    /// <summary>
    /// Verifica que el validador acepte emails con formato RFC válido.
    /// El comando es válido sin importar si el email existe en la BD
    /// (esa verificación la hace el handler).
    /// </summary>
    [Theory]
    [InlineData("user@domain.com")]
    [InlineData("juan.perez@universidad.edu")]
    [InlineData("test+tag@example.org")]
    [InlineData("USER@DOMAIN.COM")]
    public async Task Validate_WithValidEmail_ReturnsValidResult(string validEmail)
    {
        // Arrange
        var command = new ForgotPasswordCommand(validEmail);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
