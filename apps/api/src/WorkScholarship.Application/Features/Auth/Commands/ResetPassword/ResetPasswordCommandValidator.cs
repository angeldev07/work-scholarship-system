using FluentValidation;

namespace WorkScholarship.Application.Features.Auth.Commands.ResetPassword;

/// <summary>
/// Validador de FluentValidation para ResetPasswordCommand.
/// </summary>
/// <remarks>
/// Valida la presencia del token, la política de seguridad de la nueva contraseña
/// y que ambas contraseñas coincidan.
/// La validez del token en sí (existencia y expiración) la verifica el handler.
/// </remarks>
public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    /// <summary>
    /// Define las reglas de validación para el comando de reseteo de contraseña.
    /// </summary>
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("El token de recuperacion es requerido.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("La nueva contrasena es requerida.")
            .MinimumLength(8).WithMessage("La contrasena debe tener al menos 8 caracteres.")
            .Matches("[A-Z]").WithMessage("La contrasena debe contener al menos una letra mayuscula.")
            .Matches("[a-z]").WithMessage("La contrasena debe contener al menos una letra minuscula.")
            .Matches("[0-9]").WithMessage("La contrasena debe contener al menos un numero.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("La confirmacion de contrasena es requerida.")
            .Equal(x => x.NewPassword).WithMessage("Las contrasenas no coinciden.");
    }
}
