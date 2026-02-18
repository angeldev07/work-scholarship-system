using MediatR;
using Microsoft.EntityFrameworkCore;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Common.Models;

namespace WorkScholarship.Application.Features.Auth.Commands.Logout;

/// <summary>
/// Handler que procesa el comando de Logout revocando refresh tokens.
/// </summary>
/// <remarks>
/// Comportamiento:
/// - Si se proporciona un RefreshToken específico, revoca solo ese token
/// - Si no se proporciona token pero hay usuario autenticado, revoca TODOS sus tokens activos
/// - Siempre retorna éxito (incluso si el token no existe o ya estaba revocado)
/// </remarks>
public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// Inicializa el handler con las dependencias necesarias.
    /// </summary>
    /// <param name="context">Contexto de base de datos para acceder a RefreshTokens.</param>
    /// <param name="currentUserService">Servicio para obtener el usuario autenticado actual.</param>
    public LogoutCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Procesa el comando de Logout revocando uno o todos los refresh tokens.
    /// </summary>
    /// <param name="request">Comando con el refresh token opcional a revocar.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>
    /// Result exitoso siempre (operación idempotente).
    /// </returns>
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // Revoke the specific refresh token if provided
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

            if (refreshToken is not null && refreshToken.IsActive)
            {
                refreshToken.Revoke();
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        else if (_currentUserService.UserId.HasValue)
        {
            // Revoke all refresh tokens for the current user
            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == _currentUserService.UserId.Value && rt.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var token in activeTokens)
            {
                token.Revoke();
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
