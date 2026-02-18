using MediatR;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Auth.Common;

namespace WorkScholarship.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Comando para renovar un access token usando un refresh token válido.
/// </summary>
/// <param name="Token">Valor del refresh token (obtenido de la cookie httpOnly).</param>
/// <remarks>
/// Implementa token rotation: revoca el refresh token usado y genera uno nuevo.
/// Esto previene ataques de replay si un refresh token es comprometido.
/// </remarks>
public record RefreshTokenCommand(string Token) : IRequest<Result<TokenResponse>>;
