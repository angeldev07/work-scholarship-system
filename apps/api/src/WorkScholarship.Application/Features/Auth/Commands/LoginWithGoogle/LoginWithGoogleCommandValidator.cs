using FluentValidation;

namespace WorkScholarship.Application.Features.Auth.Commands.LoginWithGoogle;

/// <summary>
/// Validador de FluentValidation para LoginWithGoogleCommand.
/// </summary>
/// <remarks>
/// Valida que el authorization code y el redirect URI no estén vacíos.
/// Se ejecuta automáticamente en el pipeline de MediatR antes del handler.
/// </remarks>
public class LoginWithGoogleCommandValidator : AbstractValidator<LoginWithGoogleCommand>
{
    /// <summary>
    /// Define las reglas de validación para el comando de Login con Google.
    /// </summary>
    public LoginWithGoogleCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Authorization code es requerido.");

        RuleFor(x => x.RedirectUri)
            .NotEmpty().WithMessage("Redirect URI es requerido.");
    }
}
