namespace WorkScholarship.Domain.Enums;

/// <summary>
/// Define los proveedores de autenticación soportados por el sistema.
/// </summary>
public enum AuthProvider
{
    /// <summary>
    /// Autenticación local con email y contraseña.
    /// </summary>
    /// <remarks>
    /// El usuario tiene una contraseña hasheada almacenada en la base de datos.
    /// </remarks>
    Local = 0,

    /// <summary>
    /// Autenticación mediante Google OAuth 2.0.
    /// </summary>
    /// <remarks>
    /// El usuario se autentica usando su cuenta de Google institucional.
    /// Solo se permiten correos del dominio institucional.
    /// </remarks>
    Google = 1
}
