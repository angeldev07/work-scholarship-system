using MediatR;
using Microsoft.EntityFrameworkCore;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Auth.Common;

namespace WorkScholarship.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Handler que procesa el comando de RefreshToken para renovar tokens.
/// </summary>
/// <remarks>
/// Flujo de token rotation:
/// 1. Valida que el refresh token exista y esté activo (no revocado, no expirado)
/// 2. Verifica que el usuario asociado esté activo
/// 3. Revoca el refresh token usado (para prevenir reutilización)
/// 4. Genera nuevo access token JWT
/// 5. Genera nuevo refresh token (rotation)
/// 6. Retorna los nuevos tokens al cliente
/// </remarks>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<TokenResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITokenService _tokenService;

    /// <summary>
    /// Inicializa el handler con las dependencias necesarias.
    /// </summary>
    /// <param name="context">Contexto de base de datos para acceder a RefreshTokens y Users.</param>
    /// <param name="tokenService">Servicio para generar JWT y refresh tokens.</param>
    public RefreshTokenCommandHandler(
        IApplicationDbContext context,
        ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Procesa el comando de RefreshToken renovando los tokens del usuario.
    /// </summary>
    /// <param name="request">Comando con el refresh token a validar.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>
    /// Result con TokenResponse (nuevo access token + nuevo refresh token) si el refresh token es válido;
    /// Result.Failure con código de error específico si el refresh token es inválido.
    /// </returns>
    /// <remarks>
    /// Códigos de error posibles:
    /// - INVALID_REFRESH_TOKEN: Token inválido, revocado o expirado
    /// - SESSION_EXPIRED: Usuario no encontrado o cuenta desactivada
    /// </remarks>
    public async Task<Result<TokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return Result<TokenResponse>.Failure(
                AuthErrorCodes.INVALID_REFRESH_TOKEN,
                "Token de renovacion invalido o expirado.");
        }

        var existingToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.Token, cancellationToken);

        if (existingToken is null || !existingToken.IsActive)
        {
            return Result<TokenResponse>.Failure(
                AuthErrorCodes.INVALID_REFRESH_TOKEN,
                "Token de renovacion invalido o expirado.");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == existingToken.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Result<TokenResponse>.Failure(
                AuthErrorCodes.SESSION_EXPIRED,
                "Sesion expirada. Por favor inicia sesion nuevamente.");
        }

        // Revoke old refresh token
        existingToken.Revoke();

        // Generate new tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshTokenValue = _tokenService.GenerateRefreshToken();
        var newRefreshTokenExpiry = DateTime.UtcNow.AddDays(_tokenService.GetRefreshTokenExpirationInDays());

        user.AddRefreshToken(newRefreshTokenValue, newRefreshTokenExpiry);

        await _context.SaveChangesAsync(cancellationToken);

        var response = new TokenResponse
        {
            AccessToken = accessToken,
            ExpiresIn = _tokenService.GetAccessTokenExpirationInSeconds(),
            TokenType = "Bearer",
            RefreshTokenValue = newRefreshTokenValue,
            RefreshTokenExpirationDays = _tokenService.GetRefreshTokenExpirationInDays()
        };

        return Result<TokenResponse>.Success(response);
    }
}
