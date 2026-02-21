namespace WorkScholarship.Application.Common.Models;

/// <summary>
/// Configuración para la autenticación con Google OAuth 2.0.
/// Mapeada desde la sección "Authentication:Google" de appsettings.json.
/// </summary>
public class GoogleAuthSettings
{
    /// <summary>
    /// Nombre de la sección en appsettings.json.
    /// </summary>
    public const string SECTION_NAME = "Authentication:Google";

    /// <summary>
    /// Client ID de la aplicación registrada en Google Cloud Console.
    /// </summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// Client Secret de la aplicación registrada en Google Cloud Console.
    /// </summary>
    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>
    /// Lista de dominios de email permitidos para autenticación OAuth (ej: ["universidad.edu", "campus.edu"]).
    /// Si está vacía o nula, se permite cualquier dominio de email (útil para desarrollo).
    /// </summary>
    public List<string> AllowedDomains { get; init; } = [];

    /// <summary>
    /// URL base del frontend al que redirigir tras el callback OAuth.
    /// Ejemplo: "http://localhost:4200" en desarrollo, "https://app.universidad.edu" en producción.
    /// </summary>
    public string FrontendUrl { get; init; } = "http://localhost:4200";
}
