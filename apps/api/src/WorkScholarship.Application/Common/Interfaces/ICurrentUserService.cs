namespace WorkScholarship.Application.Common.Interfaces;

/// <summary>
/// Servicio para acceder a la información del usuario actualmente autenticado.
/// </summary>
/// <remarks>
/// Extrae información de los claims del JWT presente en la petición HTTP actual.
/// </remarks>
public interface ICurrentUserService
{
    /// <summary>
    /// Identificador único del usuario autenticado.
    /// </summary>
    /// <value>Guid del usuario o null si no hay usuario autenticado.</value>
    Guid? UserId { get; }

    /// <summary>
    /// Email del usuario autenticado.
    /// </summary>
    /// <value>Email del usuario o null si no hay usuario autenticado.</value>
    string? Email { get; }

    /// <summary>
    /// Rol del usuario autenticado.
    /// </summary>
    /// <value>Rol como string (ADMIN, SUPERVISOR, BECA) o null si no hay usuario autenticado.</value>
    string? Role { get; }

    /// <summary>
    /// Indica si hay un usuario autenticado en la petición actual.
    /// </summary>
    /// <value>True si hay un usuario autenticado; false en caso contrario.</value>
    bool IsAuthenticated { get; }
}
