using MediatR;
using Microsoft.EntityFrameworkCore;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Common.Models;

namespace WorkScholarship.Application.Features.Auth.Commands.ResetPassword;

/// <summary>
/// Handler que procesa el restablecimiento de contraseña usando un token de reset.
/// </summary>
/// <remarks>
/// Flujo:
/// 1. Busca el usuario que posee el token de reset proporcionado
/// 2. Valida que el token sea válido y no haya expirado (1 hora de vida)
/// 3. Hashea la nueva contraseña
/// 4. Llama a SetPassword() en la entidad User (limpia el token internamente)
/// 5. Revoca todos los refresh tokens activos (cierra todas las sesiones)
/// 6. Persiste los cambios en la BD
/// </remarks>
public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    /// <summary>
    /// Inicializa el handler con las dependencias necesarias.
    /// </summary>
    /// <param name="context">Contexto de base de datos para buscar el usuario por token de reset.</param>
    /// <param name="passwordHasher">Servicio para hashear la nueva contraseña.</param>
    public ResetPasswordCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// Procesa el restablecimiento de contraseña.
    /// </summary>
    /// <param name="request">Comando con token, nueva contraseña y su confirmación.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>
    /// Result.Success() si la contraseña fue restablecida exitosamente.
    /// Result.Failure(INVALID_TOKEN) si el token no existe, ya fue usado o expiró.
    /// </returns>
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(
                u => u.PasswordResetToken == request.Token,
                cancellationToken);

        if (user is null || !user.IsPasswordResetTokenValid(request.Token))
        {
            return Result.Failure(
                AuthErrorCodes.INVALID_TOKEN,
                "El enlace de recuperacion es invalido o ha expirado.");
        }

        var newPasswordHash = _passwordHasher.Hash(request.NewPassword);

        // SetPassword limpia el token de reset internamente
        user.SetPassword(newPasswordHash);

        // Revocar todas las sesiones activas por seguridad
        user.RevokeAllRefreshTokens();

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
