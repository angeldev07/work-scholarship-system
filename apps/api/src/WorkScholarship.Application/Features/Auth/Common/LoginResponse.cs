using System.Text.Json.Serialization;

namespace WorkScholarship.Application.Features.Auth.Common;

/// <summary>
/// Respuesta del comando de Login con tokens y datos del usuario.
/// </summary>
/// <remarks>
/// Incluye access token (expuesto en JSON) y refresh token (transportado internamente
/// para configurarlo como httpOnly cookie en el controlador).
/// </remarks>
public record LoginResponse
{
    /// <summary>
    /// Token JWT de acceso para autenticar peticiones subsiguientes.
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Tiempo de expiración del access token en segundos (por defecto 86400 = 24 horas).
    /// </summary>
    public int ExpiresIn { get; init; }

    /// <summary>
    /// Tipo de token (siempre "Bearer" para JWT).
    /// </summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>
    /// Datos del usuario autenticado.
    /// </summary>
    public UserDto User { get; init; } = null!;

    /// <summary>
    /// Campo interno para transportar el valor del refresh token al controlador.
    /// Excluido de la serialización JSON para que nunca llegue al body de la respuesta API.
    /// El refresh token se configura como httpOnly cookie en el controlador.
    /// </summary>
    [JsonIgnore]
    public string RefreshTokenValue { get; init; } = string.Empty;

    /// <summary>
    /// Campo interno para la expiración del refresh token en días.
    /// Usado por el controlador para configurar la expiración de la cookie.
    /// </summary>
    [JsonIgnore]
    public int RefreshTokenExpirationDays { get; init; }
}
