using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WorkScholarship.Application.Common.Email;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Auth.Common;
using WorkScholarship.Domain.Interfaces;

namespace WorkScholarship.Application.Features.Auth.Commands.ChangePassword;

/// <summary>
/// Handler que procesa el cambio de contraseña para un usuario autenticado.
/// </summary>
/// <remarks>
/// Flujo:
/// 1. Obtiene el UserId del usuario autenticado via ICurrentUserService
/// 2. Busca el usuario con sus refresh tokens
/// 3. Verifica que el usuario existe y tiene contraseña local (no es cuenta Google pura)
/// 4. Verifica que la contraseña actual es correcta
/// 5. Hashea la nueva contraseña y la persiste
/// 6. Revoca todos los refresh tokens activos (cierra sesiones en otros dispositivos)
/// 7. Genera nuevo access token + refresh token para mantener la sesión del dispositivo actual
/// 8. Envía email de confirmación de cambio de contraseña
/// 9. Retorna el nuevo access token
/// </remarks>
public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result<TokenResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly EmailSettings _emailSettings;

    /// <summary>
    /// Inicializa el handler con todas las dependencias necesarias.
    /// </summary>
    /// <param name="context">Contexto de base de datos.</param>
    /// <param name="passwordHasher">Servicio para verificar y hashear contraseñas.</param>
    /// <param name="tokenService">Servicio para generar access token y refresh token.</param>
    /// <param name="currentUserService">Servicio que expone el UserId del usuario autenticado actual.</param>
    /// <param name="emailService">Servicio para enviar email de confirmación.</param>
    /// <param name="dateTimeProvider">Proveedor de fecha/hora para generar el mensaje de confirmación.</param>
    /// <param name="emailSettings">Configuración de email (no usada en URL aquí, pero disponible para consistencia).</param>
    public ChangePasswordCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ICurrentUserService currentUserService,
        IEmailService emailService,
        IDateTimeProvider dateTimeProvider,
        IOptions<EmailSettings> emailSettings)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _currentUserService = currentUserService;
        _emailService = emailService;
        _dateTimeProvider = dateTimeProvider;
        _emailSettings = emailSettings.Value;
    }

    /// <summary>
    /// Procesa el cambio de contraseña.
    /// </summary>
    /// <param name="request">Comando con contraseña actual, nueva contraseña y confirmación.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>
    /// Result con TokenResponse (nuevo access token + refresh token) si el cambio fue exitoso.
    /// Result.Failure(UNAUTHORIZED) si no hay usuario autenticado.
    /// Result.Failure(INVALID_CURRENT_PASSWORD) si la contraseña actual es incorrecta.
    /// </returns>
    public async Task<Result<TokenResponse>> Handle(
        ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (userId is null)
        {
            return Result<TokenResponse>.Failure(
                AuthErrorCodes.UNAUTHORIZED,
                "No autorizado.");
        }

        var user = await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

        if (user is null)
        {
            return Result<TokenResponse>.Failure(
                AuthErrorCodes.UNAUTHORIZED,
                "No autorizado.");
        }

        // Los usuarios de Google sin contraseña no pueden usar este flujo
        if (user.PasswordHash is null)
        {
            return Result<TokenResponse>.Failure(
                AuthErrorCodes.INVALID_CURRENT_PASSWORD,
                "Esta cuenta no tiene contrasena local. Usa Google para autenticarte.");
        }

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return Result<TokenResponse>.Failure(
                AuthErrorCodes.INVALID_CURRENT_PASSWORD,
                "La contrasena actual es incorrecta.");
        }

        var newPasswordHash = _passwordHasher.Hash(request.NewPassword);

        user.SetPassword(newPasswordHash);

        // Revocar todos los refresh tokens → cierra sesiones en otros dispositivos
        user.RevokeAllRefreshTokens();

        // Generar nuevo par de tokens para el dispositivo actual
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var refreshTokenExpiry = _dateTimeProvider.UtcNow.AddDays(_tokenService.GetRefreshTokenExpirationInDays());

        user.AddRefreshToken(refreshTokenValue, refreshTokenExpiry);

        await _context.SaveChangesAsync(cancellationToken);

        // Email de confirmación (fire-and-forget en cuanto a errores de envío)
        var changeDate = _dateTimeProvider.UtcNow.ToString("dd/MM/yyyy HH:mm 'UTC'");
        var emailMessage = PasswordEmailTemplates.PasswordChanged(
            recipientName: user.FullName,
            recipientEmail: user.Email,
            changeDate: changeDate);

        await _emailService.SendAsync(emailMessage, cancellationToken);

        var tokenResponse = new TokenResponse
        {
            AccessToken = accessToken,
            ExpiresIn = _tokenService.GetAccessTokenExpirationInSeconds(),
            TokenType = "Bearer",
            RefreshTokenValue = refreshTokenValue,
            RefreshTokenExpirationDays = _tokenService.GetRefreshTokenExpirationInDays()
        };

        return Result<TokenResponse>.Success(tokenResponse);
    }
}
