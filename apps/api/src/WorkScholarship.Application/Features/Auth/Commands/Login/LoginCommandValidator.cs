using FluentValidation;

namespace WorkScholarship.Application.Features.Auth.Commands.Login;

/// <summary>
/// Validador de FluentValidation para LoginCommand.
/// </summary>
/// <remarks>
/// Valida formato de email y que los campos no estén vacíos.
/// Se ejecuta automáticamente en el pipeline de MediatR antes del handler.
/// </remarks>
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>
    /// Define las reglas de validación para el comando de Login.
    /// </summary>
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email es requerido.")
            .EmailAddress().WithMessage("Email no tiene un formato valido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Contrasena es requerida.");
    }
}
