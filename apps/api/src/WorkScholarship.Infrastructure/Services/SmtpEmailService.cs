using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using WorkScholarship.Application.Common.Email;
using WorkScholarship.Application.Common.Interfaces;

namespace WorkScholarship.Infrastructure.Services;

/// <summary>
/// Implementación de <see cref="IEmailService"/> que envía emails mediante SMTP usando MailKit.
/// </summary>
/// <remarks>
/// Configurada para conectarse al servidor SMTP de MailerSend en el puerto 587 con STARTTLS.
/// El remitente se toma de <see cref="EmailSettings"/> a menos que el mensaje incluya
/// <see cref="EmailMessage.FromOverride"/>, en cuyo caso ese valor tiene prioridad.
/// </remarks>
public class SmtpEmailService : IEmailService
{
    private readonly SmtpSettings _smtpSettings;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<SmtpEmailService> _logger;

    /// <summary>
    /// Inicializa el servicio con la configuración SMTP, la configuración de email general y el logger.
    /// </summary>
    /// <param name="smtpSettings">Configuración SMTP: host, puerto, credenciales y TLS.</param>
    /// <param name="emailSettings">Configuración de email: SenderName, SenderEmail y FrontendUrl.</param>
    /// <param name="logger">Logger para registrar el resultado del envío y errores.</param>
    public SmtpEmailService(
        IOptions<SmtpSettings> smtpSettings,
        IOptions<EmailSettings> emailSettings,
        ILogger<SmtpEmailService> logger)
    {
        _smtpSettings = smtpSettings.Value;
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Envía un email usando MailKit sobre SMTP con STARTTLS.
    /// </summary>
    /// <param name="message">
    /// Datos del email: destinatario, asunto, cuerpo HTML y remitente opcional.
    /// Si <see cref="EmailMessage.FromOverride"/> tiene valor, se usa como dirección del remitente.
    /// </param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <exception cref="InvalidOperationException">
    /// Se lanza si la conexión o autenticación SMTP falla.
    /// </exception>
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var mimeMessage = BuildMimeMessage(message);

        _logger.LogInformation(
            "Sending email via SMTP to {To} with subject '{Subject}'",
            message.To,
            message.Subject);

        using var client = CreateSmtpClient();

        await client.ConnectAsync(
            _smtpSettings.Host,
            _smtpSettings.Port,
            _smtpSettings.UseTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto,
            cancellationToken);

        await client.AuthenticateAsync(
            _smtpSettings.Username,
            _smtpSettings.Password,
            cancellationToken);

        await client.SendAsync(mimeMessage, cancellationToken);

        await client.DisconnectAsync(quit: true, cancellationToken);

        _logger.LogInformation(
            "Email sent successfully via SMTP to {To}",
            message.To);
    }

    /// <summary>
    /// Construye el <see cref="MimeMessage"/> de MailKit a partir del <see cref="EmailMessage"/> de la aplicación.
    /// </summary>
    /// <param name="message">Datos del email provenientes de la capa Application.</param>
    /// <returns>Mensaje MIME listo para enviarse.</returns>
    internal MimeMessage BuildMimeMessage(EmailMessage message)
    {
        var mimeMessage = new MimeMessage();

        // Remitente: usa FromOverride si está definido, de lo contrario toma EmailSettings
        if (!string.IsNullOrWhiteSpace(message.FromOverride))
        {
            mimeMessage.From.Add(MailboxAddress.Parse(message.FromOverride));
        }
        else
        {
            mimeMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
        }

        mimeMessage.To.Add(MailboxAddress.Parse(message.To));
        mimeMessage.Subject = message.Subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = message.HtmlBody
        };

        mimeMessage.Body = bodyBuilder.ToMessageBody();

        return mimeMessage;
    }

    /// <summary>
    /// Crea la instancia del cliente SMTP de MailKit.
    /// Método virtual protegido para permitir su sustitución en tests unitarios.
    /// </summary>
    /// <returns>Nueva instancia de <see cref="SmtpClient"/>.</returns>
    protected virtual SmtpClient CreateSmtpClient() => new();
}
