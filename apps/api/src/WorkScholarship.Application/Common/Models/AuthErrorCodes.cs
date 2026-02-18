namespace WorkScholarship.Application.Common.Models;

/// <summary>
/// Códigos de error estandarizados para operaciones de autenticación.
/// Usado en Result.Error.Code para identificar errores específicos.
/// </summary>
public static class AuthErrorCodes
{
    /// <summary>
    /// Error de validación de entrada (campos faltantes o formato inválido).
    /// </summary>
    public const string VALIDATION_ERROR = "VALIDATION_ERROR";

    /// <summary>
    /// Credenciales incorrectas (email o contraseña inválidos).
    /// </summary>
    public const string INVALID_CREDENTIALS = "INVALID_CREDENTIALS";

    /// <summary>
    /// Cuenta bloqueada por múltiples intentos fallidos de login.
    /// </summary>
    public const string ACCOUNT_LOCKED = "ACCOUNT_LOCKED";

    /// <summary>
    /// Intento de login local en cuenta que usa Google OAuth.
    /// </summary>
    public const string GOOGLE_ACCOUNT = "GOOGLE_ACCOUNT";

    /// <summary>
    /// Refresh token inválido, revocado o expirado.
    /// </summary>
    public const string INVALID_REFRESH_TOKEN = "INVALID_REFRESH_TOKEN";

    /// <summary>
    /// Sesión expirada, se requiere login nuevamente.
    /// </summary>
    public const string SESSION_EXPIRED = "SESSION_EXPIRED";

    /// <summary>
    /// Usuario no autenticado o token JWT inválido.
    /// </summary>
    public const string UNAUTHORIZED = "UNAUTHORIZED";

    /// <summary>
    /// Las contraseñas no coinciden (al cambiar o resetear contraseña).
    /// </summary>
    public const string PASSWORD_MISMATCH = "PASSWORD_MISMATCH";

    /// <summary>
    /// Contraseña no cumple con los requisitos de seguridad.
    /// </summary>
    public const string WEAK_PASSWORD = "WEAK_PASSWORD";

    /// <summary>
    /// Token de reseteo de contraseña inválido o expirado.
    /// </summary>
    public const string INVALID_TOKEN = "INVALID_TOKEN";

    /// <summary>
    /// Contraseña actual incorrecta al intentar cambiar contraseña.
    /// </summary>
    public const string INVALID_CURRENT_PASSWORD = "INVALID_CURRENT_PASSWORD";

    /// <summary>
    /// Límite de intentos excedido (rate limiting).
    /// </summary>
    public const string RATE_LIMIT_EXCEEDED = "RATE_LIMIT_EXCEEDED";

    /// <summary>
    /// Usuario no encontrado en la base de datos.
    /// </summary>
    public const string USER_NOT_FOUND = "USER_NOT_FOUND";

    /// <summary>
    /// Cuenta de usuario desactivada.
    /// </summary>
    public const string INACTIVE_ACCOUNT = "INACTIVE_ACCOUNT";
}
