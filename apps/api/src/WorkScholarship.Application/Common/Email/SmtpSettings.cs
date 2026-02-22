namespace WorkScholarship.Application.Common.Email;

/// <summary>
/// Configuración para el envío de emails mediante SMTP.
/// Mapeada desde la sección "Email:Smtp" de appsettings.json.
/// </summary>
/// <remarks>
/// Esta clase pertenece a la capa Application porque define los parámetros
/// de infraestructura que SmtpEmailService (Infrastructure) necesita consumir.
/// Mantener aquí la clase centraliza la configuración de email junto a EmailSettings.
/// </remarks>
public class SmtpSettings
{
    /// <summary>
    /// Nombre de la sección en appsettings.json.
    /// </summary>
    public const string SECTION_NAME = "Email:Smtp";

    /// <summary>
    /// Dirección del servidor SMTP. Ejemplo: "smtp.mailersend.net".
    /// </summary>
    public string Host { get; init; } = string.Empty;

    /// <summary>
    /// Puerto del servidor SMTP.
    /// Puerto 587 usa STARTTLS; puerto 465 usa SSL implícito.
    /// </summary>
    public int Port { get; init; } = 587;

    /// <summary>
    /// Nombre de usuario para la autenticación SMTP.
    /// </summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Contraseña para la autenticación SMTP.
    /// </summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Indica si se usa STARTTLS para cifrar la conexión.
    /// Cuando es <c>true</c>, MailKit usa <c>SecureSocketOptions.StartTls</c>.
    /// Corresponde al puerto 587. El valor por defecto es <c>true</c>.
    /// </summary>
    public bool UseTls { get; init; } = true;
}
