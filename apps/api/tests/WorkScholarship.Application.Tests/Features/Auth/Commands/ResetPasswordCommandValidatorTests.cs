using FluentAssertions;
using WorkScholarship.Application.Features.Auth.Commands.ResetPassword;

namespace WorkScholarship.Application.Tests.Features.Auth.Commands;

/// <summary>
/// Tests unitarios para ResetPasswordCommandValidator.
/// Verifica las reglas de validación del token, la política de contraseña segura
/// y la coincidencia entre nueva contraseña y confirmación.
/// </summary>
[Trait("Category", "Application")]
[Trait("Feature", "Auth")]
[Trait("Component", "ResetPasswordCommandValidator")]
public class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _validator = new();

    private const string ValidToken = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2";
    private const string ValidPassword = "SecurePass1";

    // =====================================================================
    // Token — campo requerido
    // =====================================================================

    /// <summary>
    /// Verifica que el validador rechace un token vacío o nulo,
    /// dado que el token es requerido para identificar la solicitud de reset.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_WithNullOrEmptyToken_ReturnsValidationError(string? token)
    {
        // Arrange
        var command = new ResetPasswordCommand(token!, ValidPassword, ValidPassword);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Token" && e.ErrorMessage.Contains("requerido"));
    }

    // =====================================================================
    // NewPassword — campo requerido
    // =====================================================================

    /// <summary>
    /// Verifica que el validador rechace una nueva contraseña vacía o nula.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_WithNullOrEmptyNewPassword_ReturnsValidationError(string? password)
    {
        // Arrange
        var command = new ResetPasswordCommand(ValidToken, password!, password!);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }

    // =====================================================================
    // NewPassword — política de seguridad
    // =====================================================================

    /// <summary>
    /// Verifica que el validador rechace contraseñas de menos de 8 caracteres.
    /// </summary>
    [Fact]
    public async Task Validate_WithPasswordShorterThan8Chars_ReturnsValidationError()
    {
        // Arrange — 7 caracteres con mayúscula, minúscula y dígito
        var command = new ResetPasswordCommand(ValidToken, "Short1A", "Short1A");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "NewPassword" && e.ErrorMessage.Contains("8"));
    }

    /// <summary>
    /// Verifica que el validador rechace contraseñas sin ninguna letra mayúscula.
    /// </summary>
    [Fact]
    public async Task Validate_WithPasswordWithoutUppercase_ReturnsValidationError()
    {
        // Arrange — solo minúsculas y dígito, sin mayúsculas
        var command = new ResetPasswordCommand(ValidToken, "lowercase1", "lowercase1");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "NewPassword" && e.ErrorMessage.Contains("mayuscula"));
    }

    /// <summary>
    /// Verifica que el validador rechace contraseñas sin ninguna letra minúscula.
    /// </summary>
    [Fact]
    public async Task Validate_WithPasswordWithoutLowercase_ReturnsValidationError()
    {
        // Arrange — solo mayúsculas y dígito, sin minúsculas
        var command = new ResetPasswordCommand(ValidToken, "UPPERCASE1", "UPPERCASE1");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "NewPassword" && e.ErrorMessage.Contains("minuscula"));
    }

    /// <summary>
    /// Verifica que el validador rechace contraseñas sin ningún dígito numérico.
    /// </summary>
    [Fact]
    public async Task Validate_WithPasswordWithoutDigit_ReturnsValidationError()
    {
        // Arrange — mayúscula y minúscula pero sin dígito
        var command = new ResetPasswordCommand(ValidToken, "NoDigitPass", "NoDigitPass");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "NewPassword" && e.ErrorMessage.Contains("numero"));
    }

    // =====================================================================
    // ConfirmPassword — coincidencia
    // =====================================================================

    /// <summary>
    /// Verifica que el validador rechace cuando la confirmación de contraseña
    /// no coincide con la nueva contraseña.
    /// </summary>
    [Fact]
    public async Task Validate_WithMismatchedConfirmPassword_ReturnsValidationError()
    {
        // Arrange
        var command = new ResetPasswordCommand(ValidToken, ValidPassword, "DifferentPass1");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "ConfirmPassword" && e.ErrorMessage.Contains("coinciden"));
    }

    /// <summary>
    /// Verifica que el validador rechace una confirmación vacía o nula.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_WithNullOrEmptyConfirmPassword_ReturnsValidationError(string? confirmPassword)
    {
        // Arrange
        var command = new ResetPasswordCommand(ValidToken, ValidPassword, confirmPassword!);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConfirmPassword");
    }

    // =====================================================================
    // Comando completamente válido
    // =====================================================================

    /// <summary>
    /// Verifica que un comando con token presente, contraseña que cumple toda la política
    /// de seguridad y confirmación coincidente resulta en validación exitosa.
    /// </summary>
    [Theory]
    [InlineData("Abcdef1g")]
    [InlineData("SecurePassword123")]
    [InlineData("MyStr0ngP@ss")]
    public async Task Validate_WithValidCommand_ReturnsValid(string validPassword)
    {
        // Arrange
        var command = new ResetPasswordCommand(ValidToken, validPassword, validPassword);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
