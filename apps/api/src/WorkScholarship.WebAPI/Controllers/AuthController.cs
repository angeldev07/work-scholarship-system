using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Auth.Commands.ChangePassword;
using WorkScholarship.Application.Features.Auth.Commands.ForgotPassword;
using WorkScholarship.Application.Features.Auth.Commands.Login;
using WorkScholarship.Application.Features.Auth.Commands.LoginWithGoogle;
using WorkScholarship.Application.Features.Auth.Commands.Logout;
using WorkScholarship.Application.Features.Auth.Commands.RefreshToken;
using WorkScholarship.Application.Features.Auth.Commands.ResetPassword;
using WorkScholarship.Application.Features.Auth.Common;
using WorkScholarship.Application.Features.Auth.Queries.GetCurrentUser;

namespace WorkScholarship.WebAPI.Controllers;

/// <summary>
/// Controlador REST para operaciones de autenticación y gestión de sesión.
/// </summary>
/// <remarks>
/// Endpoints implementados:
/// - POST /api/auth/login: Login con email y contraseña
/// - POST /api/auth/refresh: Renovación de access token con refresh token
/// - POST /api/auth/logout: Cierre de sesión y revocación de tokens
/// - GET /api/auth/me: Obtener datos del usuario autenticado actual
/// - GET /api/auth/google/login: Iniciar flujo de Google OAuth 2.0
/// - GET /api/auth/google/callback: Recibir callback de Google OAuth
/// - POST /api/auth/password/forgot: Solicitar recuperación de contraseña por email
/// - POST /api/auth/password/reset: Restablecer contraseña con token de reset
/// - PUT /api/auth/password/change: Cambiar contraseña (usuario autenticado)
///
/// Utiliza MediatR para enviar Commands/Queries a la capa Application.
/// Maneja conversión de Result a ApiResponse y códigos HTTP apropiados.
/// Gestiona refresh tokens como httpOnly cookies para seguridad.
/// </remarks>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly GoogleAuthSettings _googleSettings;
    private readonly bool _isSecure;

    /// <summary>
    /// Inicializa el controlador con las dependencias necesarias.
    /// </summary>
    /// <param name="sender">Sender de MediatR para enviar Commands/Queries.</param>
    /// <param name="googleAuthService">Servicio que encapsula la lógica OAuth de Google (construcción de URL, validación de tokens).</param>
    /// <param name="googleSettings">Configuración de Google OAuth (ClientId, FrontendUrl, AllowedDomains).</param>
    /// <param name="environment">Entorno de ejecución para determinar si las cookies deben ser Secure.</param>
    public AuthController(
        ISender sender,
        IGoogleAuthService googleAuthService,
        IOptions<GoogleAuthSettings> googleSettings,
        IWebHostEnvironment environment)
    {
        _sender = sender;
        _googleAuthService = googleAuthService;
        _googleSettings = googleSettings.Value;
        _isSecure = !environment.IsDevelopment();
    }

    /// <summary>
    /// Autenticar usuario con email y contraseña (login local).
    /// </summary>
    /// <param name="command">Comando con email y contraseña del usuario.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>
    /// 200 OK: ApiResponse con access token y datos del usuario. Refresh token en cookie httpOnly.
    /// 400 Bad Request: Errores de validación (VALIDATION_ERROR).
    /// 401 Unauthorized: Credenciales incorrectas (INVALID_CREDENTIALS) o cuenta bloqueada (ACCOUNT_LOCKED).
    /// 403 Forbidden: Cuenta de Google OAuth (GOOGLE_ACCOUNT) o cuenta desactivada (INACTIVE_ACCOUNT).
    /// </returns>
    /// <remarks>
    /// El access token JWT se retorna en el body de la respuesta (válido 24h).
    /// El refresh token se configura como httpOnly cookie (válido 7 días) para seguridad.
    /// </remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            var statusCode = result.Error!.Code switch
            {
                AuthErrorCodes.VALIDATION_ERROR => StatusCodes.Status400BadRequest,
                AuthErrorCodes.INVALID_CREDENTIALS => StatusCodes.Status401Unauthorized,
                AuthErrorCodes.ACCOUNT_LOCKED => StatusCodes.Status401Unauthorized,
                AuthErrorCodes.GOOGLE_ACCOUNT => StatusCodes.Status403Forbidden,
                AuthErrorCodes.INACTIVE_ACCOUNT => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };

            return StatusCode(statusCode, ApiResponse.Fail(
                result.Error.Code,
                result.Error.Message,
                result.Error.Details));
        }

        SetRefreshTokenCookie(result.Value.RefreshTokenValue, result.Value.RefreshTokenExpirationDays);

        return Ok(ApiResponse<LoginResponse>.Ok(result.Value, "Login exitoso."));
    }

    /// <summary>
    /// Iniciar flujo de autenticación con Google OAuth 2.0.
    /// Construye la URL de autorización de Google con protección CSRF y redirige al consent screen.
    /// </summary>
    /// <param name="returnUrl">URL relativa del frontend a la que redirigir después del login exitoso (ej: "/dashboard").</param>
    /// <returns>
    /// 302 Redirect: Redirige al consent screen de Google con los parámetros de OAuth.
    /// </returns>
    /// <remarks>
    /// Flujo OAuth 2.0 Authorization Code:
    /// 1. El frontend abre este endpoint (redirect completo o popup)
    /// 2. Este endpoint delega la construcción de URL a IGoogleAuthService.BuildAuthorizationUrl()
    /// 3. La URL incluye un nonce CSRF aleatorio en el parámetro state (formato: {nonce}:{returnUrl})
    /// 4. Redirige al usuario a Google para autorización
    /// 5. Google redirige de vuelta a GET /api/auth/google/callback con el code y el state
    /// </remarks>
    [HttpGet("google/login")]
    [AllowAnonymous]
    public IActionResult GoogleLogin([FromQuery] string? returnUrl = "/dashboard")
    {
        var callbackUrl = $"{Request.Scheme}://{Request.Host}/api/auth/google/callback";
        var safeReturnUrl = returnUrl ?? "/dashboard";

        var authUrl = _googleAuthService.BuildAuthorizationUrl(callbackUrl, safeReturnUrl);

        return Redirect(authUrl.Url);
    }

    /// <summary>
    /// Callback de Google OAuth 2.0. Recibe el authorization code, valida el state CSRF y procesa el login.
    /// </summary>
    /// <param name="code">Authorization code de Google (generado tras consentimiento del usuario).</param>
    /// <param name="state">Estado que contiene nonce CSRF y returnUrl (formato: {nonce}:{returnUrl}).</param>
    /// <param name="error">Error de Google OAuth (si el usuario canceló o falló la autorización).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>
    /// 302 Redirect al frontend:
    /// - Si éxito: {FrontendUrl}/auth/callback#access_token={jwt}&amp;expires_in=86400&amp;token_type=Bearer
    /// - Si error OAuth: {FrontendUrl}/auth/login?error=oauth_cancelled&amp;message={encodedMessage}
    /// - Si error de procesamiento: {FrontendUrl}/auth/login?error=oauth_failed&amp;message={encodedMessage}
    /// - Si dominio inválido: {FrontendUrl}/auth/login?error=invalid_domain&amp;message={encodedMessage}
    /// </returns>
    /// <remarks>
    /// Este endpoint es llamado por Google, NO por el frontend directamente.
    /// El state se valida mediante IGoogleAuthService.ParseStateReturnUrl() para extraer la returnUrl
    /// y verificar que el nonce tiene formato correcto (protección básica contra CSRF).
    /// El access token se pasa en el fragment (#) de la URL para que no llegue al servidor.
    /// El refresh token se configura como httpOnly cookie (SameSite=Lax por ser redirect cross-site).
    /// </remarks>
    [HttpGet("google/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken cancellationToken)
    {
        var frontendUrl = _googleSettings.FrontendUrl;

        // Si Google envió un error (usuario canceló, acceso denegado, etc.)
        if (!string.IsNullOrWhiteSpace(error))
        {
            var errorMessage = Uri.EscapeDataString("Autenticacion con Google cancelada.");
            return Redirect($"{frontendUrl}/auth/login?error=oauth_cancelled&message={errorMessage}");
        }

        // Validar el state y extraer la returnUrl — previene CSRF básico
        var returnUrl = _googleAuthService.ParseStateReturnUrl(state) ?? "/dashboard";

        // Si no hay code, el flujo OAuth está incompleto
        if (string.IsNullOrWhiteSpace(code))
        {
            var errorMessage = Uri.EscapeDataString("No se recibio codigo de autorizacion de Google.");
            return Redirect($"{frontendUrl}/auth/login?error=oauth_failed&message={errorMessage}");
        }

        // Construir redirect URI (debe coincidir exactamente con el registrado en Google Cloud Console)
        var redirectUri = $"{Request.Scheme}://{Request.Host}/api/auth/google/callback";

        var command = new LoginWithGoogleCommand(code, redirectUri);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            var errorCode = result.Error!.Code == AuthErrorCodes.INVALID_DOMAIN
                ? "invalid_domain"
                : "oauth_failed";
            var errorMessage = Uri.EscapeDataString(result.Error.Message);
            return Redirect($"{frontendUrl}/auth/login?error={errorCode}&message={errorMessage}");
        }

        // Configurar refresh token como httpOnly cookie (SameSite=Lax para redirect cross-site desde Google)
        SetRefreshTokenCookieForOAuth(result.Value.RefreshTokenValue, result.Value.RefreshTokenExpirationDays);

        // Redirigir al frontend con access token en URL fragment (no llega al servidor)
        var callbackFragment = $"access_token={result.Value.AccessToken}"
            + $"&expires_in={result.Value.ExpiresIn}"
            + "&token_type=Bearer";

        return Redirect($"{frontendUrl}/auth/callback#{callbackFragment}");
    }

    /// <summary>
    /// Renovar access token usando el refresh token almacenado en cookie.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>
    /// 200 OK: ApiResponse con nuevo access token. Nuevo refresh token en cookie (token rotation).
    /// 401 Unauthorized: Refresh token inválido, expirado o revocado (INVALID_REFRESH_TOKEN, SESSION_EXPIRED).
    /// </returns>
    /// <remarks>
    /// Implementa token rotation: revoca el refresh token usado y genera uno nuevo.
    /// Si el refresh token es inválido, elimina la cookie.
    /// </remarks>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<TokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies["refreshToken"];

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized(ApiResponse.Fail(
                AuthErrorCodes.INVALID_REFRESH_TOKEN,
                "Token de renovacion invalido o expirado."));
        }

        var command = new RefreshTokenCommand(refreshToken);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            ClearRefreshTokenCookie();

            return Unauthorized(ApiResponse.Fail(
                result.Error!.Code,
                result.Error.Message));
        }

        SetRefreshTokenCookie(result.Value.RefreshTokenValue, result.Value.RefreshTokenExpirationDays);

        return Ok(ApiResponse<TokenResponse>.Ok(result.Value, "Token renovado exitosamente."));
    }

    /// <summary>
    /// Cerrar sesión e invalidar refresh tokens del usuario.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>
    /// 200 OK: Sesión cerrada exitosamente. Cookie de refresh token eliminada.
    /// 401 Unauthorized: Usuario no autenticado (requiere JWT válido).
    /// </returns>
    /// <remarks>
    /// Revoca el refresh token específico si existe en la cookie.
    /// Si no hay cookie, revoca TODOS los refresh tokens activos del usuario.
    /// Siempre elimina la cookie del cliente.
    /// </remarks>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies["refreshToken"];

        var command = new LogoutCommand(refreshToken);
        await _sender.Send(command, cancellationToken);

        ClearRefreshTokenCookie();

        return Ok(ApiResponse.Ok("Sesion cerrada exitosamente."));
    }

    /// <summary>
    /// Obtener información del usuario actualmente autenticado.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>
    /// 200 OK: ApiResponse con UserDto del usuario autenticado.
    /// 401 Unauthorized: No hay JWT válido o usuario no encontrado (UNAUTHORIZED, USER_NOT_FOUND).
    /// </returns>
    /// <remarks>
    /// Extrae el UserId de los claims del JWT y obtiene los datos del usuario desde la BD.
    /// Útil para validar sesión y obtener datos actualizados del usuario en el frontend.
    /// </remarks>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var query = new GetCurrentUserQuery();
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            var statusCode = result.Error!.Code switch
            {
                AuthErrorCodes.UNAUTHORIZED => StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status400BadRequest
            };

            return StatusCode(statusCode, ApiResponse.Fail(
                result.Error.Code,
                result.Error.Message));
        }

        return Ok(ApiResponse<UserDto>.Ok(result.Value));
    }

    /// <summary>
    /// Solicitar recuperación de contraseña por email.
    /// </summary>
    /// <param name="command">Comando con el email del usuario.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>
    /// 200 OK: Siempre retorna éxito, sin revelar si el email existe (seguridad).
    /// 400 Bad Request: Formato de email inválido (VALIDATION_ERROR).
    /// </returns>
    /// <remarks>
    /// Si el email existe y la cuenta está activa, genera un token de reset (válido 1 hora)
    /// y envía un email con el enlace para restablecer la contraseña.
    /// Si el email no existe, retorna 200 OK igualmente para prevenir enumeración de usuarios.
    /// </remarks>
    [HttpPost("password/forgot")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(ApiResponse.Fail(
                result.Error!.Code,
                result.Error.Message,
                result.Error.Details));
        }

        return Ok(ApiResponse.Ok(
            "Se ha enviado un enlace de restablecimiento de contrasena al email solicitado."));
    }

    /// <summary>
    /// Restablecer contraseña usando el token recibido por email.
    /// </summary>
    /// <param name="command">Comando con token, nueva contraseña y confirmación.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>
    /// 200 OK: Contraseña restablecida exitosamente.
    /// 400 Bad Request: Token inválido/expirado (INVALID_TOKEN), contraseñas no coinciden (PASSWORD_MISMATCH),
    ///                  contraseña débil (WEAK_PASSWORD) o errores de validación (VALIDATION_ERROR).
    /// </returns>
    /// <remarks>
    /// El token tiene vigencia de 1 hora y se invalida tras ser usado.
    /// Tras el reset, todas las sesiones activas del usuario son revocadas.
    /// </remarks>
    [HttpPost("password/reset")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(ApiResponse.Fail(
                result.Error!.Code,
                result.Error.Message,
                result.Error.Details));
        }

        return Ok(ApiResponse.Ok(
            "Contrasena restablecida exitosamente. Puedes iniciar sesion con tu nueva contrasena."));
    }

    /// <summary>
    /// Cambiar contraseña de un usuario autenticado.
    /// </summary>
    /// <param name="command">Comando con contraseña actual, nueva contraseña y confirmación.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>
    /// 200 OK: Contraseña cambiada. Nuevo access token + refresh token en cookie.
    /// 400 Bad Request: Errores de validación (VALIDATION_ERROR, PASSWORD_MISMATCH, WEAK_PASSWORD).
    /// 401 Unauthorized: No autenticado o contraseña actual incorrecta (UNAUTHORIZED, INVALID_CURRENT_PASSWORD).
    /// </returns>
    /// <remarks>
    /// Requiere JWT válido. Tras el cambio:
    /// - Todas las sesiones en otros dispositivos son revocadas.
    /// - Se genera nuevo access token + refresh token para el dispositivo actual.
    /// - El nuevo refresh token se configura en httpOnly cookie.
    /// </remarks>
    [HttpPut("password/change")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<TokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            var statusCode = result.Error!.Code is AuthErrorCodes.UNAUTHORIZED
                or AuthErrorCodes.INVALID_CURRENT_PASSWORD
                ? StatusCodes.Status401Unauthorized
                : StatusCodes.Status400BadRequest;

            return StatusCode(statusCode, ApiResponse.Fail(
                result.Error.Code,
                result.Error.Message,
                result.Error.Details));
        }

        SetRefreshTokenCookie(result.Value.RefreshTokenValue, result.Value.RefreshTokenExpirationDays);

        return Ok(ApiResponse<TokenResponse>.Ok(
            result.Value,
            "Contrasena cambiada exitosamente. Tu sesion ha sido renovada."));
    }

    /// <summary>
    /// Configura el refresh token como httpOnly cookie en la respuesta HTTP.
    /// </summary>
    /// <param name="refreshToken">Valor del refresh token a almacenar.</param>
    /// <param name="expirationDays">Días hasta la expiración de la cookie.</param>
    /// <remarks>
    /// Opciones de seguridad de la cookie:
    /// - HttpOnly: Previene acceso desde JavaScript (protección contra XSS)
    /// - Secure: Solo se envía por HTTPS
    /// - SameSite: Strict (protección contra CSRF)
    /// - Path: /api/auth (solo enviada en endpoints de autenticación)
    /// </remarks>
    private void SetRefreshTokenCookie(string refreshToken, int expirationDays)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = _isSecure,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(expirationDays),
            Path = "/api/auth"
        };

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

    /// <summary>
    /// Configura el refresh token como httpOnly cookie con SameSite=Lax para OAuth redirects.
    /// </summary>
    /// <param name="refreshToken">Valor del refresh token a almacenar.</param>
    /// <param name="expirationDays">Días hasta la expiración de la cookie.</param>
    /// <remarks>
    /// Usa SameSite=Lax en lugar de Strict porque el callback de Google OAuth es un redirect
    /// cross-site (Google → backend → frontend). Con Strict, la cookie no se establecería
    /// correctamente al redirigir al frontend.
    /// </remarks>
    private void SetRefreshTokenCookieForOAuth(string refreshToken, int expirationDays)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(expirationDays),
            Path = "/api/auth"
        };

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

    /// <summary>
    /// Elimina la cookie del refresh token de la respuesta HTTP.
    /// </summary>
    /// <remarks>
    /// Usa las mismas opciones de cookie que SetRefreshTokenCookie para asegurar
    /// que la cookie correcta sea eliminada en el cliente.
    /// </remarks>
    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = _isSecure,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth"
        });
    }
}
