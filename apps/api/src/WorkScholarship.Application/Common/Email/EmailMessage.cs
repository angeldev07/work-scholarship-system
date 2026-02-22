namespace WorkScholarship.Application.Common.Email;

/// <summary>
/// Encapsula todos los datos necesarios para enviar un email.
/// </summary>
/// <param name="To">Dirección de correo del destinatario.</param>
/// <param name="Subject">Asunto del email.</param>
/// <param name="HtmlBody">Cuerpo del email en formato HTML.</param>
/// <param name="FromOverride">Remitente personalizado (opcional, usa el configurado por defecto si es nulo).</param>
public record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? FromOverride = null);
