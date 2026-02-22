using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;
using AppEmail = WorkScholarship.Application.Common.Email;
using WorkScholarship.Application.Common.Interfaces;

namespace WorkScholarship.Infrastructure.Services;

/// <summary>
/// Implementación de IEmailService que usa el SDK de Resend para enviar emails.
/// </summary>
/// <remarks>
/// Resend es el proveedor de email configurado para el sistema.
/// El API token se configura mediante ResendClientOptions en DI.
/// La dirección del remitente se toma de EmailSettings.
/// </remarks>
public class ResendEmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly AppEmail.EmailSettings _emailSettings;
    private readonly ILogger<ResendEmailService> _logger;

    /// <summary>
    /// Inicializa el servicio con el cliente de Resend y la configuración de email.
    /// </summary>
    /// <param name="resend">Cliente de Resend inyectado por el SDK.</param>
    /// <param name="emailSettings">Configuración con SenderName, SenderEmail y FrontendUrl.</param>
    /// <param name="logger">Logger para registrar el resultado del envío.</param>
    public ResendEmailService(
        IResend resend,
        IOptions<AppEmail.EmailSettings> emailSettings,
        ILogger<ResendEmailService> logger)
    {
        _resend = resend;
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Envía un email usando el SDK de Resend.
    /// </summary>
    /// <param name="message">Datos del email encapsulados (destinatario, asunto, cuerpo HTML, remitente opcional).</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <remarks>
    /// El remitente usa el formato "Nombre &lt;email@dominio.com&gt;".
    /// Si el EmailMessage tiene FromOverride, se usa ese valor como dirección del remitente.
    /// </remarks>
    public async Task SendAsync(AppEmail.EmailMessage message, CancellationToken cancellationToken = default)
    {
        var fromAddress = string.IsNullOrWhiteSpace(message.FromOverride)
            ? $"{_emailSettings.SenderName} <{_emailSettings.SenderEmail}>"
            : message.FromOverride;

        var resendMessage = new Resend.EmailMessage
        {
            From = fromAddress,
            Subject = message.Subject,
            HtmlBody = message.HtmlBody
        };

        resendMessage.To.Add(message.To);

        _logger.LogInformation(
            "Sending email to {To} with subject '{Subject}'",
            message.To,
            message.Subject);

        await _resend.EmailSendAsync(resendMessage);

        _logger.LogInformation(
            "Email sent successfully to {To}",
            message.To);
    }
}
