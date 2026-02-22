using FluentAssertions;
using WorkScholarship.Application.Features.Auth.Commands.ChangePassword;

namespace WorkScholarship.Application.Tests.Features.Auth.Commands;

/// <summary>
/// Tests unitarios para ChangePasswordCommandValidator.
/// Verifica las reglas de validación para la contraseña actual, la política de seguridad
/// de la nueva contraseña, la restricción de igualdad con la actual,
/// y la coincidencia entre nueva contraseña y confirmación.
/// </summary>
[Trait("Category", "Application")]
[Trait("Feature", "Auth")]
[Trait("Component", "ChangePasswordCommandValidator")]
public class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator = new();

    private const string ValidCurrentPassword = "CurrentPass1";
    private const string ValidNewPassword = "NewSecurePass1";

    // =====================================================================
    // CurrentPassword — campo requerido
    // =====================================================================

    /// <summary>
    /// Verifica que el validador rechace cuando la contraseña actual es vacía o nula.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_WithNullOrEmptyCurrentPassword_ReturnsValidationError(string? currentPassword)
    {
        // Arrange
        var command = new ChangePasswordCommand(currentPassword!, ValidNewPassword, ValidNewPassword);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "CurrentPassword" && e.ErrorMessage.Contains("requerida"));
    }

    // =====================================================================
    // NewPassword — campo requerido
    // =====================================================================

    /// <summary>
    /// Verifica que el validador rechace cuando la nueva contraseña es vacía o nula.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_WithNullOrEmptyNewPassword_ReturnsValidationError(string? newPassword)
    {
        // Arrange
        var command = new ChangePasswordCommand(ValidCurrentPassword, newPassword!, newPassword!);

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
    /// Verifica que el validador rechace una nueva contraseña de menos de 8 caracteres.
    /// </summary>
    [Fact]
    public async Task Validate_WithNewPasswordShorterThan8Chars_ReturnsValidationError()
    {
        // Arrange
        var command = new ChangePasswordCommand(ValidCurrentPassword, "Short1A", "Short1A");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "NewPassword" && e.ErrorMessage.Contains("8"));
    }

    /// <summary>
    /// Verifica que el validador rechace una nueva contraseña sin letra mayúscula.
    /// </summary>
    [Fact]
    public async Task Validate_WithNewPasswordWithoutUppercase_ReturnsValidationError()
    {
        // Arrange
        var command = new ChangePasswordCommand(ValidCurrentPassword, "lowercase1", "lowercase1");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "NewPassword" && e.ErrorMessage.Contains("mayuscula"));
    }

    /// <summary>
    /// Verifica que el validador rechace una nueva contraseña sin letra minúscula.
    /// </summary>
    [Fact]
    public async Task Validate_WithNewPasswordWithoutLowercase_ReturnsValidationError()
    {
        // Arrange
        var command = new ChangePasswordCommand(ValidCurrentPassword, "UPPERCASE1", "UPPERCASE1");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "NewPassword" && e.ErrorMessage.Contains("minuscula"));
    }

    /// <summary>
    /// Verifica que el validador rechace una nueva contraseña sin dígito numérico.
    /// </summary>
    [Fact]
    public async Task Validate_WithNewPasswordWithoutDigit_ReturnsValidationError()
    {
        // Arrange
        var command = new ChangePasswordCommand(ValidCurrentPassword, "NoDigitPass", "NoDigitPass");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "NewPassword" && e.ErrorMessage.Contains("numero"));
    }

    // =====================================================================
    // NewPassword — no puede ser igual a la actual
    // =====================================================================

    /// <summary>
    /// Verifica que el validador rechace cuando la nueva contraseña es idéntica a la actual.
    /// Cambiar por la misma contraseña no incrementa la seguridad.
    /// </summary>
    [Fact]
    public async Task Validate_WithNewPasswordEqualToCurrentPassword_ReturnsValidationError()
    {
        // Arrange — misma contraseña cumple la política pero no debe aceptarse
        var command = new ChangePasswordCommand("SamePass1", "SamePass1", "SamePass1");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "NewPassword" && e.ErrorMessage.Contains("igual"));
    }

    // =====================================================================
    // ConfirmPassword — coincidencia
    // =====================================================================

    /// <summary>
    /// Verifica que el validador rechace cuando la confirmación de contraseña
    /// no coincide exactamente con la nueva contraseña.
    /// </summary>
    [Fact]
    public async Task Validate_WithMismatchedConfirmPassword_ReturnsValidationError()
    {
        // Arrange
        var command = new ChangePasswordCommand(ValidCurrentPassword, ValidNewPassword, "DifferentPass1");

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
        var command = new ChangePasswordCommand(ValidCurrentPassword, ValidNewPassword, confirmPassword!);

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
    /// Verifica que un comando con contraseña actual presente, nueva contraseña que cumple
    /// toda la política de seguridad, diferente a la actual, y con confirmación coincidente,
    /// resulta en validación exitosa.
    /// </summary>
    [Theory]
    [InlineData("OldPass1", "NewSecurePass1", "NewSecurePass1")]
    [InlineData("Current1Pass", "Abcdef1g", "Abcdef1g")]
    [InlineData("PreviousP1", "MyStr0ngPass", "MyStr0ngPass")]
    public async Task Validate_WithValidCommand_ReturnsValid(
        string currentPassword,
        string newPassword,
        string confirmPassword)
    {
        // Arrange
        var command = new ChangePasswordCommand(currentPassword, newPassword, confirmPassword);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
