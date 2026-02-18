using System.Text.Json.Serialization;

namespace WorkScholarship.Application.Features.Auth.Common;

/// <summary>
/// Respuesta del comando de RefreshToken con nuevos tokens.
/// </summary>
/// <remarks>
/// Implementa token rotation: cada vez que se usa un refresh token, se invalida
/// el anterior y se genera uno nuevo (transportado internamente para configurarlo como cookie).
/// </remarks>
public record TokenResponse
{
    /// <summary>
    /// Nuevo token JWT de acceso generado.
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Tiempo de expiración del nuevo access token en segundos (por defecto 86400 = 24 horas).
    /// </summary>
    public int ExpiresIn { get; init; }

    /// <summary>
    /// Tipo de token (siempre "Bearer" para JWT).
    /// </summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>
    /// Campo interno para transportar el valor del nuevo refresh token al controlador.
    /// Excluido de la serialización JSON. El controlador lo configura como httpOnly cookie.
    /// </summary>
    [JsonIgnore]
    public string RefreshTokenValue { get; init; } = string.Empty;

    /// <summary>
    /// Campo interno para la expiración del nuevo refresh token en días.
    /// </summary>
    [JsonIgnore]
    public int RefreshTokenExpirationDays { get; init; }
}
