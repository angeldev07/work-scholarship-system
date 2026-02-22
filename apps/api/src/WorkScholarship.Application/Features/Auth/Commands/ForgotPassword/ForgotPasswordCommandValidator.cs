using FluentValidation;

namespace WorkScholarship.Application.Features.Auth.Commands.ForgotPassword;

/// <summary>
/// Validador de FluentValidation para ForgotPasswordCommand.
/// </summary>
/// <remarks>
/// Solo valida que el email tenga formato válido.
/// No se valida si el email existe en la BD (eso corresponde al handler por seguridad).
/// </remarks>
public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    /// <summary>
    /// Define las reglas de validación para el comando de recuperación de contraseña.
    /// </summary>
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es requerido.")
            .EmailAddress().WithMessage("El email no tiene un formato valido.");
    }
}
