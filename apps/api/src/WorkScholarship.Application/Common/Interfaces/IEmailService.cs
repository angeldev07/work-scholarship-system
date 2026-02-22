using WorkScholarship.Application.Common.Email;

namespace WorkScholarship.Application.Common.Interfaces;

/// <summary>
/// Contrato para el servicio de envío de emails.
/// </summary>
/// <remarks>
/// Las implementaciones concretas residen en Infrastructure.
/// Los handlers de Application dependen de esta interfaz, no del proveedor específico (Resend, SendGrid, etc.).
/// </remarks>
public interface IEmailService
{
    /// <summary>
    /// Envía un email con los datos encapsulados en el mensaje.
    /// </summary>
    /// <param name="message">Datos del email: destinatario, asunto, cuerpo HTML y remitente opcional.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
