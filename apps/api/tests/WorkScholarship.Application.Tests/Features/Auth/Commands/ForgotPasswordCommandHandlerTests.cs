using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using WorkScholarship.Application.Common.Email;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Features.Auth.Commands.ForgotPassword;
using WorkScholarship.Application.Tests.TestHelpers;
using WorkScholarship.Domain.Interfaces;
using WorkScholarship.Infrastructure.Data;

namespace WorkScholarship.Application.Tests.Features.Auth.Commands;

/// <summary>
/// Tests unitarios para ForgotPasswordCommandHandler.
/// Verifica la prevención de enumeración de usuarios, la generación correcta del token,
/// el envío del email de recuperación y la persistencia del token en la BD.
/// </summary>
[Trait("Category", "Application")]
[Trait("Feature", "Auth")]
[Trait("Component", "ForgotPasswordCommandHandler")]
public class ForgotPasswordCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IOptions<EmailSettings> _emailSettings;
    private readonly ForgotPasswordCommandHandler _handler;

    private static readonly DateTime _fixedNow = new(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

    public ForgotPasswordCommandHandlerTests()
    {
        _dbContext = DbContextFactory.CreateInMemoryContext();
        _emailService = Substitute.For<IEmailService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _dateTimeProvider.UtcNow.Returns(_ => _fixedNow);

        _emailSettings = Options.Create(new EmailSettings
        {
            FrontendUrl = "http://localhost:4200",
            SenderName = "Work Scholarship System",
            SenderEmail = "noreply@worksholarship.dev"
        });

        _handler = new ForgotPasswordCommandHandler(
            _dbContext,
            _emailService,
            _dateTimeProvider,
            _emailSettings);
    }

    public void Dispose() => _dbContext.Dispose();

    // =====================================================================
    // Prevención de enumeración de usuarios
    // =====================================================================

    /// <summary>
    /// Verifica que cuando el email no existe en la BD, el handler retorna
    /// Result.Success() sin ejecutar ninguna acción (prevención de enumeración de usuarios).
    /// </summary>
    [Fact]
    public async Task Handle_WithNonExistentEmail_ReturnsSuccessWithoutSendingEmail()
    {
        // Arrange
        var command = new ForgotPasswordCommand("nonexistent@univ.edu");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _emailService.DidNotReceive().SendAsync(
            Arg.Any<EmailMessage>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifica que cuando el usuario existe pero está inactivo, el handler retorna
    /// Result.Success() sin enviar email (prevención de enumeración de usuarios).
    /// </summary>
    [Fact]
    public async Task Handle_WithInactiveUser_ReturnsSuccessWithoutSendingEmail()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("inactive@univ.edu")
            .AsInactive()
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var command = new ForgotPasswordCommand("inactive@univ.edu");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _emailService.DidNotReceive().SendAsync(
            Arg.Any<EmailMessage>(),
            Arg.Any<CancellationToken>());
    }

    // =====================================================================
    // Happy path — usuario activo encontrado
    // =====================================================================

    /// <summary>
    /// Verifica que con un usuario activo, el handler retorna Result.Success()
    /// y envía exactamente un email de recuperación.
    /// </summary>
    [Fact]
    public async Task Handle_WithActiveUser_ReturnsSuccessAndSendsEmail()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("active@univ.edu")
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var command = new ForgotPasswordCommand("active@univ.edu");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _emailService.Received(1).SendAsync(
            Arg.Any<EmailMessage>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifica que el email de recuperación se envía a la dirección correcta del usuario.
    /// </summary>
    [Fact]
    public async Task Handle_WithActiveUser_SendsEmailToCorrectAddress()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("user@univ.edu")
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var command = new ForgotPasswordCommand("user@univ.edu");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _emailService.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.To == "user@univ.edu"),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifica que el enlace de reset en el cuerpo del email apunta a la FrontendUrl
    /// configurada en EmailSettings e incluye el token como query param.
    /// </summary>
    [Fact]
    public async Task Handle_WithActiveUser_SendsEmailWithResetUrlContainingToken()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("user@univ.edu")
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var command = new ForgotPasswordCommand("user@univ.edu");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — el cuerpo HTML debe contener la URL del frontend con el parámetro token
        await _emailService.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m =>
                m.HtmlBody.Contains("http://localhost:4200/auth/reset-password?token=")),
            Arg.Any<CancellationToken>());
    }

    // =====================================================================
    // Normalización del email
    // =====================================================================

    /// <summary>
    /// Verifica que el handler normaliza el email a minúsculas antes de buscar
    /// el usuario, permitiendo que un email en mayúsculas coincida con el registro en BD.
    /// </summary>
    [Fact]
    public async Task Handle_WithUppercaseEmail_NormalizesToLowercaseForLookup()
    {
        // Arrange — usuario almacenado con email en minúsculas
        var user = new UserBuilder()
            .WithEmail("user@univ.edu")
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // El comando llega con email en mayúsculas
        var command = new ForgotPasswordCommand("USER@UNIV.EDU");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert — debe encontrar el usuario y enviar el email
        result.IsSuccess.Should().BeTrue();
        await _emailService.Received(1).SendAsync(
            Arg.Any<EmailMessage>(),
            Arg.Any<CancellationToken>());
    }

    // =====================================================================
    // Persistencia del token de reset
    // =====================================================================

    /// <summary>
    /// Verifica que el token de reset se persiste en la BD con expiración de exactamente
    /// 1 hora desde el momento actual devuelto por IDateTimeProvider.
    /// </summary>
    [Fact]
    public async Task Handle_WithActiveUser_PersistsResetTokenWithOneHourExpiry()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("user@univ.edu")
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var command = new ForgotPasswordCommand("user@univ.edu");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedUser = await _dbContext.Users.FirstAsync(u => u.Email == "user@univ.edu");
        updatedUser.PasswordResetToken.Should().NotBeNullOrEmpty();
        updatedUser.PasswordResetTokenExpiresAt.Should().Be(_fixedNow.AddHours(1));
    }

    /// <summary>
    /// Verifica que el token de reset generado es un string hexadecimal de 128 caracteres
    /// (64 bytes × 2 chars/byte) en minúsculas, adecuado para uso seguro.
    /// </summary>
    [Fact]
    public async Task Handle_WithActiveUser_GeneratesHexTokenOf128Characters()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("user@univ.edu")
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var command = new ForgotPasswordCommand("user@univ.edu");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedUser = await _dbContext.Users.FirstAsync(u => u.Email == "user@univ.edu");
        updatedUser.PasswordResetToken!.Length.Should().Be(128);
        updatedUser.PasswordResetToken.Should().MatchRegex("^[0-9a-f]+$");
    }

    /// <summary>
    /// Verifica que llamadas sucesivas al handler generan tokens criptográficamente distintos.
    /// Previene ataques de predicción de tokens al asegurar la aleatoriedad del generador.
    /// </summary>
    [Fact]
    public async Task Handle_CalledTwice_GeneratesDifferentTokensEachTime()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("user@univ.edu")
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var command = new ForgotPasswordCommand("user@univ.edu");

        // Act — primera llamada
        await _handler.Handle(command, CancellationToken.None);
        var token1 = (await _dbContext.Users.FirstAsync(u => u.Email == "user@univ.edu")).PasswordResetToken;

        // Act — segunda llamada
        await _handler.Handle(command, CancellationToken.None);
        var token2 = (await _dbContext.Users.FirstAsync(u => u.Email == "user@univ.edu")).PasswordResetToken;

        // Assert
        token1.Should().NotBe(token2);
    }
}
