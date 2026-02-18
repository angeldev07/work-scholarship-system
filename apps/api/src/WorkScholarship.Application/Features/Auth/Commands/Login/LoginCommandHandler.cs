using MediatR;
using Microsoft.EntityFrameworkCore;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Auth.Common;
using WorkScholarship.Domain.Enums;

namespace WorkScholarship.Application.Features.Auth.Commands.Login;

/// <summary>
/// Handler que procesa el comando de Login (autenticación local con email y contraseña).
/// </summary>
/// <remarks>
/// Flujo:
/// 1. Busca usuario por email (normalizado a lowercase)
/// 2. Valida estado de cuenta (activa, no bloqueada)
/// 3. Verifica que no sea cuenta de Google sin contraseña
/// 4. Verifica la contraseña con hash almacenado
/// 5. Genera access token JWT y refresh token
/// 6. Actualiza LastLoginAt y reinicia contadores de intentos fallidos
/// 7. Retorna tokens y datos del usuario
/// </remarks>
public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    /// <summary>
    /// Inicializa el handler con las dependencias necesarias.
    /// </summary>
    /// <param name="context">Contexto de base de datos para acceder a Users y RefreshTokens.</param>
    /// <param name="passwordHasher">Servicio para verificar el hash de la contraseña.</param>
    /// <param name="tokenService">Servicio para generar JWT y refresh tokens.</param>
    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Procesa el comando de Login validando credenciales y generando tokens.
    /// </summary>
    /// <param name="request">Comando con email y contraseña del usuario.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>
    /// Result con LoginResponse (tokens + datos del usuario) si el login es exitoso;
    /// Result.Failure con código de error específico si falla la autenticación.
    /// </returns>
    /// <remarks>
    /// Códigos de error posibles:
    /// - INVALID_CREDENTIALS: Email o contraseña incorrectos
    /// - INACTIVE_ACCOUNT: Cuenta desactivada
    /// - GOOGLE_ACCOUNT: Intento de login local en cuenta de Google OAuth
    /// - ACCOUNT_LOCKED: Cuenta bloqueada por múltiples intentos fallidos (15 min lockout)
    /// </remarks>
    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
        {
            return Result<LoginResponse>.Failure(
                AuthErrorCodes.INVALID_CREDENTIALS,
                "Email o contrasena incorrectos.");
        }

        if (!user.IsActive)
        {
            return Result<LoginResponse>.Failure(
                AuthErrorCodes.INACTIVE_ACCOUNT,
                "Tu cuenta esta desactivada. Contacta al administrador.");
        }

        if (user.AuthProvider == AuthProvider.Google && user.PasswordHash is null)
        {
            return Result<LoginResponse>.Failure(
                AuthErrorCodes.GOOGLE_ACCOUNT,
                "Esta cuenta usa Google. Usa 'Iniciar sesion con Google'.");
        }

        if (user.IsLockedOut())
        {
            return Result<LoginResponse>.Failure(
                AuthErrorCodes.ACCOUNT_LOCKED,
                "Cuenta bloqueada por intentos fallidos. Intenta en 15 minutos.");
        }

        if (user.PasswordHash is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin();
            await _context.SaveChangesAsync(cancellationToken);

            return Result<LoginResponse>.Failure(
                AuthErrorCodes.INVALID_CREDENTIALS,
                "Email o contrasena incorrectos.");
        }

        // Successful login
        user.RecordSuccessfulLogin();

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(_tokenService.GetRefreshTokenExpirationInDays());

        user.AddRefreshToken(refreshTokenValue, refreshTokenExpiry);

        await _context.SaveChangesAsync(cancellationToken);

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
