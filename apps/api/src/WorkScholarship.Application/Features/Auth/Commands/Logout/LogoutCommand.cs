using MediatR;
using WorkScholarship.Application.Common.Models;

namespace WorkScholarship.Application.Features.Auth.Commands.Logout;

/// <summary>
/// Comando para cerrar sesión de un usuario revocando refresh tokens.
/// </summary>
/// <param name="RefreshToken">
/// Refresh token opcional a revocar (extraído de la cookie httpOnly).
/// Si se proporciona, revoca solo ese token específico.
/// Si es null, revoca todos los tokens activos del usuario autenticado.
/// </param>
/// <remarks>
/// La revocación de tokens previene que puedan ser usados nuevamente.
/// El access token JWT no se revoca (está en memoria del cliente), pero expira en 24h.
/// </remarks>
public record LogoutCommand(string? RefreshToken) : IRequest<Result>;
