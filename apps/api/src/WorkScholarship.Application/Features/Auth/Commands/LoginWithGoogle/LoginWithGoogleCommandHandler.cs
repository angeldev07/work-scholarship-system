using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Auth.Common;
using WorkScholarship.Domain.Entities;
using WorkScholarship.Domain.Enums;

namespace WorkScholarship.Application.Features.Auth.Commands.LoginWithGoogle;

/// <summary>
/// Handler que procesa el comando de Login con Google OAuth 2.0.
/// </summary>
/// <remarks>
/// Flujo:
/// 1. Intercambia el authorization code por datos del usuario de Google
/// 2. Valida dominio de email si AllowedDomains está configurado
/// 3. Busca usuario existente por email
/// 4. Si no existe → crea nuevo usuario con User.CreateFromGoogle()
/// 5. Si existe con AuthProvider.Local → vincula cuenta con User.LinkGoogleAccount()
/// 6. Si existe con AuthProvider.Google → login directo
/// 7. Verifica que la cuenta esté activa
/// 8. Genera JWT access token + refresh token
/// 9. Retorna LoginResponse
/// </remarks>
public class LoginWithGoogleCommandHandler : IRequestHandler<LoginWithGoogleCommand, Result<LoginResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly ITokenService _tokenService;
    private readonly GoogleAuthSettings _googleSettings;

    /// <summary>
    /// Inicializa el handler con las dependencias necesarias.
    /// </summary>
    /// <param name="context">Contexto de base de datos.</param>
    /// <param name="googleAuthService">Servicio para intercambiar código OAuth por datos de usuario.</param>
    /// <param name="tokenService">Servicio para generar JWT y refresh tokens.</param>
    /// <param name="googleSettings">Configuración de Google OAuth (dominio permitido).</param>
    public LoginWithGoogleCommandHandler(
        IApplicationDbContext context,
        IGoogleAuthService googleAuthService,
        ITokenService tokenService,
        IOptions<GoogleAuthSettings> googleSettings)
    {
        _context = context;
        _googleAuthService = googleAuthService;
        _tokenService = tokenService;
        _googleSettings = googleSettings.Value;
    }

    /// <summary>
    /// Procesa el comando de Login con Google OAuth.
    /// </summary>
    /// <param name="request">Comando con el authorization code de Google.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>
    /// Result con LoginResponse (tokens + datos del usuario) si el login es exitoso;
    /// Result.Failure con código de error específico si falla.
    /// </returns>
    /// <remarks>
    /// Códigos de error posibles:
    /// - OAUTH_FAILED: Error al intercambiar el código con Google
    /// - INVALID_DOMAIN: Dominio de email no permitido
    /// - INACTIVE_ACCOUNT: Cuenta desactivada
    /// </remarks>
    public async Task<Result<LoginResponse>> Handle(
        LoginWithGoogleCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Intercambiar authorization code por datos de usuario de Google
        var googleUser = await _googleAuthService.ExchangeCodeForUserInfoAsync(
            request.Code,
            request.RedirectUri,
            cancellationToken);

        if (googleUser is null)
        {
            return Result<LoginResponse>.Failure(
                AuthErrorCodes.OAUTH_FAILED,
                "Error al autenticar con Google. Intenta nuevamente.");
        }

        // 2. Validar dominio de email si hay dominios configurados
        if (_googleSettings.AllowedDomains is { Count: > 0 })
        {
            var emailDomain = googleUser.Email.Split('@').LastOrDefault();

            var isAllowed = _googleSettings.AllowedDomains
                .Any(d => string.Equals(emailDomain, d, StringComparison.OrdinalIgnoreCase));

            if (!isAllowed)
            {
                var domains = string.Join(", ", _googleSettings.AllowedDomains.Select(d => $"@{d}"));
                return Result<LoginResponse>.Failure(
                    AuthErrorCodes.INVALID_DOMAIN,
                    $"Solo correos de los dominios {domains} son permitidos.");
            }
        }

        // 3. Buscar usuario existente por email
        var normalizedEmail = googleUser.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null)
        {
            // 4. Usuario nuevo → crear con Google
            user = User.CreateFromGoogle(
                googleUser.Email,
                googleUser.FirstName,
                googleUser.LastName,
                googleUser.GoogleId,
                googleUser.PhotoUrl,
                "google-oauth");

            _context.Users.Add(user);
        }
        else if (user.AuthProvider == AuthProvider.Local)
        {
            // 5. Usuario existente con login local → vincular cuenta Google
            user.LinkGoogleAccount(googleUser.GoogleId, googleUser.PhotoUrl);
        }
        // 6. Si ya es Google → login directo, no se necesita hacer nada especial

        // 7. Verificar cuenta activa
        if (!user.IsActive)
        {
            return Result<LoginResponse>.Failure(
                AuthErrorCodes.INACTIVE_ACCOUNT,
                "Tu cuenta esta desactivada. Contacta al administrador.");
        }

        // 8. Registrar login exitoso y generar tokens
        user.RecordSuccessfulLogin();

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(_tokenService.GetRefreshTokenExpirationInDays());

        user.AddRefreshToken(refreshTokenValue, refreshTokenExpiry);

        await _context.SaveChangesAsync(cancellationToken);

        // 9. Construir y retornar respuesta
        var response = new LoginResponse
        {
            AccessToken = accessToken,
            ExpiresIn = _tokenService.GetAccessTokenExpirationInSeconds(),
            TokenType = "Bearer",
            User = UserDto.FromEntity(user),
            RefreshTokenValue = refreshTokenValue,
            RefreshTokenExpirationDays = _tokenService.GetRefreshTokenExpirationInDays()
        };

        return Result<LoginResponse>.Success(response);
    }
}
