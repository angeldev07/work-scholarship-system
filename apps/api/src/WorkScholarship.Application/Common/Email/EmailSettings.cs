namespace WorkScholarship.Application.Common.Email;

/// <summary>
/// Configuración general del servicio de email.
/// Mapeada desde la sección "Email" de appsettings.json.
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// Nombre de la sección en appsettings.json.
    /// </summary>
    public const string SECTION_NAME = "Email";

    /// <summary>
    /// URL base del frontend para construir enlaces en los emails.
    /// Ejemplo: "http://localhost:4200" en desarrollo.
    /// </summary>
    public string FrontendUrl { get; init; } = "http://localhost:4200";

    /// <summary>
    /// Nombre del remitente que aparece en los emails enviados.
    /// </summary>
    public string SenderName { get; init; } = "Work Scholarship System";

    /// <summary>
    /// Dirección de email del remitente.
    /// </summary>
    public string SenderEmail { get; init; } = "noreply@worksholarship.dev";
}
