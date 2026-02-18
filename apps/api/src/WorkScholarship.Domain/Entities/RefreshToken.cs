using WorkScholarship.Domain.Common;

namespace WorkScholarship.Domain.Entities;

/// <summary>
/// Representa un token de actualización (refresh token) para renovar tokens de acceso JWT.
/// </summary>
/// <remarks>
/// Los refresh tokens se almacenan en cookies httpOnly y permiten obtener nuevos access tokens
/// sin requerir credenciales nuevamente. Implementa rotación de tokens por seguridad.
/// </remarks>
public class RefreshToken : BaseEntity
{
    private RefreshToken() { }

    /// <summary>
    /// Crea un nuevo refresh token.
    /// </summary>
    /// <param name="userId">Identificador del usuario propietario del token.</param>
    /// <param name="token">Valor único del token.</param>
    /// <param name="expiresAt">Fecha y hora de expiración del token.</param>
    /// <param name="ipAddress">Dirección IP desde donde se generó el token (opcional).</param>
    /// <returns>Nueva instancia de RefreshToken.</returns>
    /// <exception cref="ArgumentException">
    /// Si userId es Guid.Empty, token está vacío, o expiresAt no está en el futuro.
    /// </exception>
    public static RefreshToken Create(Guid userId, string token, DateTime expiresAt, string? ipAddress = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token is required.", nameof(token));
        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Expiration date must be in the future.", nameof(expiresAt));

        return new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Identificador del usuario propietario del token.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Valor único del refresh token.
    /// </summary>
    public string Token { get; private set; } = string.Empty;

    /// <summary>
    /// Fecha y hora de expiración del token.
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// Fecha y hora en que el token fue revocado (nulo si no ha sido revocado).
    /// </summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>
    /// Dirección IP desde donde se generó el token.
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// Indica si el token ha expirado.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Indica si el token ha sido revocado manualmente.
    /// </summary>
    public bool IsRevoked => RevokedAt.HasValue;

    /// <summary>
    /// Indica si el token está activo (no revocado y no expirado).
    /// </summary>
    public bool IsActive => !IsRevoked && !IsExpired;

    /// <summary>
    /// Revoca el token, marcándolo como inválido.
    /// </summary>
    /// <remarks>
    /// Usado durante logout, cambio de contraseña o rotación de tokens.
    /// No hace nada si el token ya está revocado.
    /// </remarks>
    public void Revoke()
    {
        if (IsRevoked)
            return;

        RevokedAt = DateTime.UtcNow;
    }
}
