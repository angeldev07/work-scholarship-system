using FluentValidation;

namespace WorkScholarship.Application.Features.Auth.Commands.ChangePassword;

/// <summary>
/// Validador de FluentValidation para ChangePasswordCommand.
/// </summary>
/// <remarks>
/// Valida la presencia de la contraseña actual, la política de seguridad de la nueva contraseña
/// y que ambas contraseñas nuevas coincidan.
/// La verificación de que la contraseña actual sea correcta la realiza el handler.
/// </remarks>
public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    /// <summary>
    /// Define las reglas de validación para el comando de cambio de contraseña.
    /// </summary>
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("La contrasena actual es requerida.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("La nueva contrasena es requerida.")
            .MinimumLength(8).WithMessage("La contrasena debe tener al menos 8 caracteres.")
            .Matches("[A-Z]").WithMessage("La contrasena debe contener al menos una letra mayuscula.")
            .Matches("[a-z]").WithMessage("La contrasena debe contener al menos una letra minuscula.")
            .Matches("[0-9]").WithMessage("La contrasena debe contener al menos un numero.")
            .NotEqual(x => x.CurrentPassword).WithMessage("La nueva contrasena no puede ser igual a la actual.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("La confirmacion de contrasena es requerida.")
            .Equal(x => x.NewPassword).WithMessage("Las contrasenas no coinciden.");
    }
}
