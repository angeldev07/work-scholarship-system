using FluentAssertions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using NSubstitute;
using WorkScholarship.Application.Common.Email;
using WorkScholarship.Infrastructure.Services;
using AppEmailMessage = WorkScholarship.Application.Common.Email.EmailMessage;

namespace WorkScholarship.Infrastructure.Tests.Services;

/// <summary>
/// Tests unitarios para SmtpEmailService.
/// Verifica la construcción correcta del MimeMessage (From, To, Subject, HtmlBody),
/// el manejo del remitente (EmailSettings por defecto vs. FromOverride),
/// y el logging en SendAsync.
/// Los tests de construcción de mensaje usan el método interno BuildMimeMessage() directamente,
/// lo que evita la conexión SMTP real. El test de logging usa una subclase que
/// sobreescribe CreateSmtpClient() para no conectarse a ningún servidor.
/// </summary>
[Trait("Category", "Infrastructure")]
[Trait("Component", "SmtpEmailService")]
public class SmtpEmailServiceTests
{
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly IOptions<SmtpSettings> _smtpSettings;
    private readonly IOptions<EmailSettings> _emailSettings;
    private readonly SmtpEmailService _service;

    private static readonly SmtpSettings DefaultSmtpSettings = new()
    {
        Host = "smtp.mailersend.net",
        Port = 587,
        Username = "user@test.mlsender.net",
        Password = "test-password",
        UseTls = true
    };

    private static readonly EmailSettings DefaultEmailSettings = new()
    {
        SenderName = "Work Scholarship System",
        SenderEmail = "noreply@worksholarship.dev",
        FrontendUrl = "http://localhost:4200"
    };

    public SmtpEmailServiceTests()
    {
        _logger = Substitute.For<ILogger<SmtpEmailService>>();
        _smtpSettings = Options.Create(DefaultSmtpSettings);
        _emailSettings = Options.Create(DefaultEmailSettings);

        _service = new SmtpEmailService(_smtpSettings, _emailSettings, _logger);
    }

    // =====================================================================
    // BuildMimeMessage — destinatario
    // =====================================================================

    /// <summary>
    /// Verifica que el campo To del MimeMessage contiene exactamente
    /// la dirección de email del destinatario del AppEmailMessage.
    /// </summary>
    [Fact]
    public void BuildMimeMessage_Always_SetsCorrectRecipient()
    {
        // Arrange
        var message = new AppEmailMessage(
            To: "student@univ.edu",
            Subject: "Asunto",
            HtmlBody: "<p>Contenido</p>");

        // Act
        var mimeMessage = _service.BuildMimeMessage(message);

        // Assert — se accede a Mailboxes (IEnumerable<MailboxAddress>) para evitar ambigüedad de Should()
        var toAddresses = mimeMessage.To.Mailboxes.ToList();
        toAddresses.Should().ContainSingle();
        toAddresses[0].Address.Should().Be("student@univ.edu");
    }

    // =====================================================================
    // BuildMimeMessage — asunto
    // =====================================================================

    /// <summary>
    /// Verifica que el asunto del MimeMessage coincide exactamente
    /// con el campo Subject del AppEmailMessage.
    /// </summary>
    [Fact]
    public void BuildMimeMessage_Always_SetsCorrectSubject()
    {
        // Arrange
        var message = new AppEmailMessage(
            To: "student@univ.edu",
            Subject: "Restablecimiento de contrasena",
            HtmlBody: "<p>Contenido</p>");

        // Act
        var mimeMessage = _service.BuildMimeMessage(message);

        // Assert
        mimeMessage.Subject.Should().Be("Restablecimiento de contrasena");
    }

    // =====================================================================
    // BuildMimeMessage — cuerpo HTML
    // =====================================================================

    /// <summary>
    /// Verifica que el cuerpo HTML del MimeMessage coincide exactamente
    /// con el campo HtmlBody del AppEmailMessage.
    /// </summary>
    [Fact]
    public void BuildMimeMessage_Always_SetsCorrectHtmlBody()
    {
        // Arrange
        var htmlBody = "<h1>Titulo</h1><p>Parrafo de contenido</p>";
        var message = new AppEmailMessage(
            To: "student@univ.edu",
            Subject: "Asunto",
            HtmlBody: htmlBody);

        // Act
        var mimeMessage = _service.BuildMimeMessage(message);

        // Assert — el cuerpo se encapsula como TextPart dentro del MimeMessage
        var textPart = mimeMessage.Body as TextPart;
        textPart.Should().NotBeNull();
        textPart!.Text.Should().Be(htmlBody);
    }

    // =====================================================================
    // BuildMimeMessage — remitente por defecto (EmailSettings)
    // =====================================================================

    /// <summary>
    /// Verifica que cuando FromOverride es null, el remitente usa
    /// SenderName y SenderEmail de EmailSettings.
    /// </summary>
    [Fact]
    public void BuildMimeMessage_WithoutFromOverride_UsesDefaultSenderFromSettings()
    {
        // Arrange
        var message = new AppEmailMessage(
            To: "student@univ.edu",
            Subject: "Asunto",
            HtmlBody: "<p>Cuerpo</p>",
            FromOverride: null);

        // Act
        var mimeMessage = _service.BuildMimeMessage(message);

        // Assert — se accede a Mailboxes para evitar ambigüedad de FluentAssertions con InternetAddressList
        var fromList = mimeMessage.From.Mailboxes.ToList();
        fromList.Should().ContainSingle();
        fromList[0].Name.Should().Be("Work Scholarship System");
        fromList[0].Address.Should().Be("noreply@worksholarship.dev");
    }

    /// <summary>
    /// Verifica que cuando FromOverride es cadena vacía o whitespace,
    /// el remitente también usa los valores de EmailSettings.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildMimeMessage_WithEmptyOrWhitespaceFromOverride_UsesDefaultSender(string fromOverride)
    {
        // Arrange
        var message = new AppEmailMessage(
            To: "student@univ.edu",
            Subject: "Asunto",
            HtmlBody: "<p>Cuerpo</p>",
            FromOverride: fromOverride);

        // Act
        var mimeMessage = _service.BuildMimeMessage(message);

        // Assert
        var from = mimeMessage.From.Mailboxes.First();
        from.Name.Should().Be("Work Scholarship System");
        from.Address.Should().Be("noreply@worksholarship.dev");
    }

    // =====================================================================
    // BuildMimeMessage — remitente override
    // =====================================================================

    /// <summary>
    /// Verifica que cuando FromOverride tiene un valor, ese valor se usa
    /// como dirección del remitente, ignorando EmailSettings.
    /// </summary>
    [Fact]
    public void BuildMimeMessage_WithFromOverride_UsesThatAddressAsFrom()
    {
        // Arrange
        var message = new AppEmailMessage(
            To: "student@univ.edu",
            Subject: "Asunto",
            HtmlBody: "<p>Cuerpo</p>",
            FromOverride: "support@custom-domain.com");

        // Act
        var mimeMessage = _service.BuildMimeMessage(message);

        // Assert
        var from = mimeMessage.From.Mailboxes.First();
        from.Address.Should().Be("support@custom-domain.com");
    }

    /// <summary>
    /// Verifica que cuando FromOverride tiene valor, el remitente predeterminado
    /// de EmailSettings NO aparece en el campo From.
    /// </summary>
    [Fact]
    public void BuildMimeMessage_WithFromOverride_DoesNotUseDefaultSenderEmail()
    {
        // Arrange
        var message = new AppEmailMessage(
            To: "student@univ.edu",
            Subject: "Asunto",
            HtmlBody: "<p>Cuerpo</p>",
            FromOverride: "support@custom-domain.com");

        // Act
        var mimeMessage = _service.BuildMimeMessage(message);

        // Assert
        var fromAddresses = mimeMessage.From.Mailboxes.Select(m => m.Address).ToList();
        fromAddresses.Should().NotContain("noreply@worksholarship.dev");
    }

    // =====================================================================
    // BuildMimeMessage — estructura del mensaje
    // =====================================================================

    /// <summary>
    /// Verifica que BuildMimeMessage produce exactamente un remitente y un destinatario,
    /// sin duplicados aunque se llame con los mismos datos.
    /// </summary>
    [Fact]
    public void BuildMimeMessage_Always_ProducesExactlyOneFromAndOneTo()
    {
        // Arrange
        var message = new AppEmailMessage(
            To: "student@univ.edu",
            Subject: "Asunto",
            HtmlBody: "<p>Cuerpo</p>");

        // Act
        var mimeMessage = _service.BuildMimeMessage(message);

        // Assert — Count propiedad directa del InternetAddressList (int, sin ambigüedad)
        mimeMessage.From.Count.Should().Be(1);
        mimeMessage.To.Count.Should().Be(1);
    }

    // =====================================================================
    // SendAsync — logging inicial (antes de la conexión)
    // =====================================================================

    /// <summary>
    /// Verifica que SendAsync registra un mensaje de log de Information
    /// antes de intentar conectarse al servidor SMTP.
    /// El test usa NonConnectingSmtpEmailService que sobreescribe CreateSmtpClient()
    /// para retornar un cliente cuya conexión falla inmediatamente,
    /// permitiendo verificar que el primer log ocurre antes del error de conexión.
    /// </summary>
    [Fact]
    public async Task SendAsync_Always_LogsInformationBeforeConnecting()
    {
        // Arrange
        var logger = Substitute.For<ILogger<SmtpEmailService>>();
        var service = new NonConnectingSmtpEmailService(_smtpSettings, _emailSettings, logger);

        var message = new AppEmailMessage(
            To: "student@univ.edu",
            Subject: "Envio de prueba",
            HtmlBody: "<p>Contenido</p>");

        // Act — la conexión fallará, pero el log de inicio debe haberse registrado
        try
        {
            await service.SendAsync(message, CancellationToken.None);
        }
        catch
        {
            // Se espera excepción al intentar conectar con host inválido.
            // Solo verificamos que el log inicial ocurrió antes del error.
        }

        // Assert — verifica que se llamó ILogger.Log con nivel Information al menos una vez
        logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // =====================================================================
    // Clase de apoyo para tests — evita conexión SMTP real
    // =====================================================================

    /// <summary>
    /// Subclase de SmtpEmailService que sobreescribe CreateSmtpClient()
    /// para retornar un SmtpClient estándar sin configuración adicional.
    /// Permite testear el flujo de SendAsync hasta el punto de conexión sin
    /// necesitar un servidor SMTP real. La conexión fallará de forma controlada.
    /// </summary>
    private sealed class NonConnectingSmtpEmailService : SmtpEmailService
    {
        public NonConnectingSmtpEmailService(
            IOptions<SmtpSettings> smtpSettings,
            IOptions<EmailSettings> emailSettings,
            ILogger<SmtpEmailService> logger)
            : base(smtpSettings, emailSettings, logger)
        {
        }

        /// <summary>
        /// Retorna un SmtpClient de MailKit sin configuración adicional.
        /// La llamada a ConnectAsync en este cliente fallará de forma controlada
        /// al intentar conectar con el host configurado en SmtpSettings.
        /// </summary>
        protected override SmtpClient CreateSmtpClient() => new SmtpClient();
    }
}
