using MediatR;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Auth.Common;

namespace WorkScholarship.Application.Features.Auth.Commands.ChangePassword;

/// <summary>
/// Comando para cambiar la contraseña de un usuario autenticado.
/// </summary>
/// <param name="CurrentPassword">Contraseña actual del usuario (requerida para verificar identidad).</param>
/// <param name="NewPassword">Nueva contraseña que reemplazará a la actual.</param>
/// <param name="ConfirmPassword">Confirmación de la nueva contraseña (debe coincidir con NewPassword).</param>
/// <remarks>
/// Requiere que el usuario esté autenticado. El UserId se obtiene del ICurrentUserService en el handler.
/// Tras el cambio exitoso, retorna un nuevo access token + refresh token para mantener la sesión activa.
/// </remarks>
public record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword) : IRequest<Result<TokenResponse>>;
