using MediatR;
using WorkScholarship.Application.Common.Models;

namespace WorkScholarship.Application.Features.Auth.Commands.ResetPassword;

/// <summary>
/// Comando para restablecer la contraseña usando un token de reset.
/// </summary>
/// <param name="Token">Token criptográfico recibido por email (válido por 1 hora).</param>
/// <param name="NewPassword">Nueva contraseña que debe cumplir la política de seguridad.</param>
/// <param name="ConfirmPassword">Confirmación de la nueva contraseña (debe coincidir con NewPassword).</param>
public record ResetPasswordCommand(
    string Token,
    string NewPassword,
    string ConfirmPassword) : IRequest<Result>;
