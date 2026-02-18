using WorkScholarship.Domain.Entities;

namespace WorkScholarship.Application.Common.Interfaces;

/// <summary>
/// Servicio para generación y configuración de tokens JWT y refresh tokens.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Genera un token de acceso JWT para el usuario especificado.
    /// </summary>
    /// <param name="user">Usuario para el cual generar el token.</param>
    /// <returns>Token JWT firmado como string.</returns>
    /// <remarks>
    /// El token incluye claims como: sub (userId), email, role, firstName, lastName.
    /// </remarks>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Genera un refresh token criptográficamente seguro.
    /// </summary>
    /// <returns>String aleatorio base64 de 64 bytes.</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Obtiene el tiempo de expiración del access token en segundos.
    /// </summary>
    /// <returns>Segundos hasta la expiración (por defecto 86400 = 24 horas).</returns>
    int GetAccessTokenExpirationInSeconds();

    /// <summary>
    /// Obtiene el tiempo de expiración del refresh token en días.
    /// </summary>
    /// <returns>Días hasta la expiración (por defecto 7 días).</returns>
    int GetRefreshTokenExpirationInDays();
}
