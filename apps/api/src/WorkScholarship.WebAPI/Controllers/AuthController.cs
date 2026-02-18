using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Auth.Commands.Login;
using WorkScholarship.Application.Features.Auth.Commands.Logout;
using WorkScholarship.Application.Features.Auth.Commands.RefreshToken;
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

    /// <summary>
    /// Inicializa el controlador con el sender de MediatR.
    /// </summary>
    /// <param name="sender">Sender de MediatR para enviar Commands/Queries.</param>
    public AuthController(ISender sender)
    {
        _sender = sender;
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

        // Set refresh token as httpOnly cookie
        SetRefreshTokenCookie(result.Value.RefreshTokenValue, result.Value.RefreshTokenExpirationDays);

        return Ok(ApiResponse<LoginResponse>.Ok(result.Value, "Login exitoso."));
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

        // Rotate refresh token cookie
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
            Secure = true,
            SameSite = SameSiteMode.Strict,
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
    /// que la cookie correcta sea eliminada.
    /// </remarks>
    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth"
        });
    }
}
