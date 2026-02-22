namespace WorkScholarship.Application.Common.Email;

/// <summary>
/// Templates de email para flujos relacionados con gestión de contraseñas.
/// </summary>
/// <remarks>
/// Cada método construye el HTML del contenido específico, aplica los parámetros mediante
/// <see cref="EmailTemplateBuilder"/> y retorna un <see cref="EmailMessage"/> listo para enviar.
/// El layout base (banner + footer) lo agrega <see cref="EmailTemplateBuilder.WrapInLayout"/>.
/// </remarks>
public static class PasswordEmailTemplates
{
    /// <summary>
    /// Template del cuerpo del email de recuperación de contraseña.
    /// </summary>
    private const string PASSWORD_RESET_TEMPLATE = """
        <h2 style="margin:0 0 16px;color:#1e293b;font-size:22px;">Recupera tu contraseña</h2>
        <p style="margin:0 0 12px;color:#475569;font-size:15px;line-height:1.6;">
          Hola <strong>{{RecipientName}}</strong>,
        </p>
        <p style="margin:0 0 24px;color:#475569;font-size:15px;line-height:1.6;">
          Recibimos una solicitud para restablecer la contraseña de tu cuenta.
          Si no realizaste esta solicitud, puedes ignorar este mensaje con seguridad.
        </p>
        <p style="margin:0 0 24px;color:#475569;font-size:15px;line-height:1.6;">
          Para crear una nueva contraseña, haz clic en el siguiente botón:
        </p>
        <table cellpadding="0" cellspacing="0" style="margin:0 0 28px;">
          <tr>
            <td style="border-radius:6px;background-color:#1e40af;">
              <a href="{{ResetUrl}}"
                 target="_blank"
                 style="display:inline-block;padding:14px 32px;color:#ffffff;font-size:15px;font-weight:bold;text-decoration:none;border-radius:6px;">
                Restablecer contraseña
              </a>
            </td>
          </tr>
        </table>
        <p style="margin:0 0 6px;color:#64748b;font-size:13px;line-height:1.6;">
          O copia y pega este enlace en tu navegador:
        </p>
        <p style="margin:0 0 24px;">
          <a href="{{ResetUrl}}" style="color:#2563eb;font-size:13px;word-break:break-all;">{{ResetUrl}}</a>
        </p>
        <div style="background-color:#fef3c7;border-left:4px solid #f59e0b;padding:14px 16px;border-radius:4px;">
          <p style="margin:0;color:#92400e;font-size:13px;font-weight:bold;">Este enlace expira en 1 hora.</p>
          <p style="margin:4px 0 0;color:#92400e;font-size:13px;">
            Por seguridad, el enlace se invalida tras ser utilizado.
          </p>
        </div>
        """;

    /// <summary>
    /// Template del cuerpo del email de confirmación de cambio de contraseña.
    /// </summary>
    private const string PASSWORD_CHANGED_TEMPLATE = """
        <h2 style="margin:0 0 16px;color:#1e293b;font-size:22px;">Contraseña actualizada</h2>
        <p style="margin:0 0 12px;color:#475569;font-size:15px;line-height:1.6;">
          Hola <strong>{{RecipientName}}</strong>,
        </p>
        <p style="margin:0 0 24px;color:#475569;font-size:15px;line-height:1.6;">
          Tu contraseña fue actualizada exitosamente el {{ChangeDate}}.
          Todas las sesiones activas en otros dispositivos fueron cerradas por seguridad.
        </p>
        <div style="background-color:#dcfce7;border-left:4px solid #16a34a;padding:14px 16px;border-radius:4px;margin-bottom:24px;">
          <p style="margin:0;color:#14532d;font-size:13px;font-weight:bold;">Puedes iniciar sesión con tu nueva contraseña.</p>
        </div>
        <p style="margin:0;color:#64748b;font-size:13px;line-height:1.6;">
          Si no realizaste este cambio, contacta al administrador inmediatamente.
        </p>
        """;

    /// <summary>
    /// Construye el EmailMessage para solicitud de recuperación de contraseña.
    /// </summary>
    /// <param name="recipientName">Nombre del destinatario para personalizar el saludo.</param>
    /// <param name="recipientEmail">Dirección de email del destinatario.</param>
    /// <param name="resetUrl">URL completa hacia la página de reset (ya incluye el token como query param).</param>
    /// <returns>EmailMessage listo para enviar a través de IEmailService.</returns>
    public static EmailMessage PasswordReset(
        string recipientName,
        string recipientEmail,
        string resetUrl)
    {
        const string subject = "Restablece tu contraseña - Work Scholarship System";

        var contentHtml = EmailTemplateBuilder.ApplyPlaceholders(
            PASSWORD_RESET_TEMPLATE,
            new Dictionary<string, string>
            {
                { "RecipientName", recipientName },
                { "ResetUrl", resetUrl }
            });

        var htmlBody = EmailTemplateBuilder.WrapInLayout(subject, contentHtml);

        return new EmailMessage(
            To: recipientEmail,
            Subject: subject,
            HtmlBody: htmlBody);
    }

    /// <summary>
    /// Construye el EmailMessage de confirmación cuando la contraseña fue cambiada exitosamente.
    /// </summary>
    /// <param name="recipientName">Nombre del destinatario.</param>
    /// <param name="recipientEmail">Dirección de email del destinatario.</param>
    /// <param name="changeDate">Fecha y hora legible del cambio (ej: "21/02/2026 10:35 UTC").</param>
    /// <returns>EmailMessage listo para enviar a través de IEmailService.</returns>
    public static EmailMessage PasswordChanged(
        string recipientName,
        string recipientEmail,
        string changeDate)
    {
        const string subject = "Tu contraseña fue cambiada - Work Scholarship System";

        var contentHtml = EmailTemplateBuilder.ApplyPlaceholders(
            PASSWORD_CHANGED_TEMPLATE,
            new Dictionary<string, string>
            {
                { "RecipientName", recipientName },
                { "ChangeDate", changeDate }
            });

        var htmlBody = EmailTemplateBuilder.WrapInLayout(subject, contentHtml);

        return new EmailMessage(
            To: recipientEmail,
            Subject: subject,
            HtmlBody: htmlBody);
    }
}
