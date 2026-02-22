using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Resend;
using WorkScholarship.Application.Common.Email;
using WorkScholarship.Infrastructure.Services;
using AppEmailMessage = WorkScholarship.Application.Common.Email.EmailMessage;

namespace WorkScholarship.Infrastructure.Tests.Services;

/// <summary>
/// Tests unitarios para ResendEmailService.
/// Verifica la construcción correcta del EmailMessage del SDK de Resend,
/// el manejo del remitente (default vs override), y la invocación de IResend.EmailSendAsync().
/// IResend es mockeado con NSubstitute para aislar el test de la red.
/// Los argumentos se verifican directamente con Arg.Is en Received().
/// </summary>
[Trait("Category", "Infrastructure")]
[Trait("Component", "ResendEmailService")]
public class ResendEmailServiceTests
{
    private readonly IResend _resend;
    private readonly ILogger<ResendEmailService> _logger;
    private readonly IOptions<EmailSettings> _emailSettings;
    private readonly ResendEmailService _service;

    private static readonly EmailSettings DefaultSettings = new()
    {
        SenderName = "Work Scholarship System",
        SenderEmail = "noreply@worksholarship.dev",
        FrontendUrl = "http://localhost:4200"
    };

    public ResendEmailServiceTests()
    {
        _resend = Substitute.For<IResend>();
        _logger = Substitute.For<ILogger<ResendEmailService>>();
        _emailSettings = Options.Create(DefaultSettings);

        _service = new ResendEmailService(_resend, _emailSettings, _logger);
    }

    // =====================================================================
    // Invocación del SDK
    // =====================================================================

    /// <summary>
    /// Verifica que SendAsync invoca IResend.EmailSendAsync() exactamente una vez,
    /// delegando el envío al SDK de Resend.
    /// </summary>
    [Fact]
    public async Task SendAsync_Always_CallsResendEmailSendAsyncOnce()
    {
        // Arrange
        var message = new AppEmailMessage(
            To: "recipient@univ.edu",
            Subject: "Asunto de prueba",
            HtmlBody: "<p>Cuerpo del email</p>");

        // Act
        await _service.SendAsync(message, CancellationToken.None);

        // Assert
        await _resend.Received(1).EmailSendAsync(
            Arg.Any<Resend.EmailMessage>(),
            Arg.Any<CancellationToken>());
    }

    // =====================================================================
    // Campos del mensaje
    // =====================================================================

    /// <summary>
    /// Verifica que el destinatario del EmailMessage enviado al SDK coincide
    /// exactamente con el campo To del AppEmailMessage de la aplicación.
    /// </summary>
    [Fact]
    public async Task SendAsync_Always_SetsCorrectRecipient()
    {
        // Arrange
        var message = new AppEmailMessage(
            To: "target@univ.edu",
            Subject: "Asunto",
            HtmlBody: "<p>Contenido</p>");

        // Act
        await _service.SendAsync(message, CancellationToken.None);

        // Assert — verificar el destinatario via Arg.Is en la llamada recibida
        await _resend.Received(1).EmailSendAsync(
            Arg.Is<Resend.EmailMessage>(m => m.To.Contains("target@univ.edu")),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifica que el asunto del EmailMessage enviado al SDK coincide
    /// exactamente con el campo Subject del AppEmailMessage.
    /// </summary>
    [Fact]
    public async Task SendAsync_Always_SetsCorrectSubject()
    {
        // Arrange
        var message = new AppEmailMessage(
            To: "recipient@univ.edu",
            Subject: "Restablecimiento de contrasena",
            HtmlBody: "<p>Contenido</p>");

        // Act
        await _service.SendAsync(message, CancellationToken.None);

        // Assert
        await _resend.Received(1).EmailSendAsync(
            Arg.Is<Resend.EmailMessage>(m => m.Subject == "Restablecimiento de contrasena"),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifica que el cuerpo HTML del EmailMessage enviado al SDK coincide
    /// exactamente con el campo HtmlBody del AppEmailMessage.
    /// </summary>
    [Fact]
    public async Task SendAsync_Always_SetsCorrectHtmlBody()
    {
        // Arrange
        var htmlBody = "<h1>Titulo</h1><p>Parrafo de contenido</p>";
        var message = new AppEmailMessage(
            To: "recipient@univ.edu",
            Subject: "Asunto",
            HtmlBody: htmlBody);

        // Act
        await _service.SendAsync(message, CancellationToken.None);

        // Assert
        await _resend.Received(1).EmailSendAsync(
            Arg.Is<Resend.EmailMessage>(m => m.HtmlBody == htmlBody),
            Arg.Any<CancellationToken>());
    }

    // =====================================================================
    // Remitente — configuración por defecto
    // =====================================================================

    /// <summary>
    /// Verifica que cuando FromOverride es null, el remitente se construye
    /// con el formato "Nombre &lt;email@dominio.com&gt;" usando los valores de EmailSettings.
    /// El campo From del SDK puede ser un tipo especial, se compara como string.
    /// </summary>
    [Fact]
    public async Task SendAsync_WithoutFromOverride_UsesDefaultSenderFromSettings()
    {
        // Arrange
        var message = new AppEmailMessage(
            To: "recipient@univ.edu",
            Subject: "Asunto",
            HtmlBody: "<p>Cuerpo</p>",
            FromOverride: null);

        // Act
        await _service.SendAsync(message, CancellationToken.None);

        // Assert — From debe contener nombre y email del remitente configurado
        await _resend.Received(1).EmailSendAsync(
            Arg.Is<Resend.EmailMessage>(m =>
                m.From.ToString().Contains("Work Scholarship System") &&
                m.From.ToString().Contains("noreply@worksholarship.dev")),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifica que cuando FromOverride es una cadena vacía o whitespace,
    /// el remitente también usa los valores de EmailSettings (no deja el From vacío).
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendAsync_WithEmptyOrWhitespaceFromOverride_UsesDefaultSender(string fromOverride)
    {
        // Arrange
        var message = new AppEmailMessage(
            To: "recipient@univ.edu",
            Subject: "Asunto",
            HtmlBody: "<p>Cuerpo</p>",
            FromOverride: fromOverride);

        // Act
        await _service.SendAsync(message, CancellationToken.None);

        // Assert
        await _resend.Received(1).EmailSendAsync(
            Arg.Is<Resend.EmailMessage>(m =>
                m.From.ToString().Contains("Work Scholarship System") &&
                m.From.ToString().Contains("noreply@worksholarship.dev")),
            Arg.Any<CancellationToken>());
    }

    // =====================================================================
    // Remitente — override
    // =====================================================================

    /// <summary>
    /// Verifica que cuando FromOverride tiene un valor, se usa directamente
    /// como dirección del remitente, ignorando los valores de EmailSettings.
    /// </summary>
    [Fact]
    public async Task SendAsync_WithFromOverride_UsesThatAddressInstead()
    {
        // Arrange
        var message = new AppEmailMessage(
            To: "recipient@univ.edu",
            Subject: "Asunto",
            HtmlBody: "<p>Cuerpo</p>",
            FromOverride: "support@custom-domain.com");

        // Act
        await _service.SendAsync(message, CancellationToken.None);

        // Assert
        await _resend.Received(1).EmailSendAsync(
            Arg.Is<Resend.EmailMessage>(m =>
                m.From.ToString().Contains("support@custom-domain.com")),
            Arg.Any<CancellationToken>());
    }
}
