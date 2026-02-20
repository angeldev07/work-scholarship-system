using WorkScholarship.Domain.Entities;
using WorkScholarship.Domain.Enums;

namespace WorkScholarship.Application.Features.Auth.Common;

/// <summary>
/// DTO que representa los datos de un usuario para respuestas de la API.
/// </summary>
/// <remarks>
/// Abstrae la entidad User del dominio para exponerla de forma segura en la API.
/// No incluye información sensible como password hash o refresh tokens.
/// </remarks>
public record UserDto
{
    /// <summary>
    /// Identificador único del usuario.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Dirección de correo electrónico del usuario.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Nombre(s) del usuario.
    /// </summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>
    /// Apellido(s) del usuario.
    /// </summary>
    public string LastName { get; init; } = string.Empty;

    /// <summary>
    /// Nombre completo del usuario (FirstName + LastName).
    /// </summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>
    /// Rol del usuario en el sistema (ADMIN, SUPERVISOR, BECA, NONE).
    /// </summary>
    public UserRole Role { get; init; } = UserRole.None;

    /// <summary>
    /// URL de la foto de perfil del usuario (null si no tiene foto).
    /// </summary>
    public string? PhotoUrl { get; init; }

    /// <summary>
    /// Indica si la cuenta del usuario está activa.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Fecha y hora del último login exitoso (null si nunca ha iniciado sesión).
    /// </summary>
    public DateTime? LastLogin { get; init; }

    /// <summary>
    /// Proveedor de autenticación usado por el usuario (Local o Google).
    /// </summary>
    public string AuthProvider { get; init; } = string.Empty;

    /// <summary>
    /// Convierte una entidad User del dominio a UserDto para la API.
    /// </summary>
    /// <param name="user">Entidad User a convertir.</param>
    /// <returns>UserDto con los datos del usuario.</returns>
    public static UserDto FromEntity(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        FullName = user.FullName,
        Role = user.Role,
        PhotoUrl = user.PhotoUrl,
        IsActive = user.IsActive,
        LastLogin = user.LastLoginAt,
        AuthProvider = user.AuthProvider.ToString()
    };
}
