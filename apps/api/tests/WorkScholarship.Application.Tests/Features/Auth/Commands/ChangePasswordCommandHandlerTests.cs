using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using WorkScholarship.Application.Common.Email;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Auth.Commands.ChangePassword;
using WorkScholarship.Application.Features.Auth.Common;
using WorkScholarship.Application.Tests.TestHelpers;
using WorkScholarship.Domain.Interfaces;
using WorkScholarship.Infrastructure.Data;

namespace WorkScholarship.Application.Tests.Features.Auth.Commands;

/// <summary>
/// Tests unitarios para ChangePasswordCommandHandler.
/// Verifica la autenticación del usuario, la validación de la contraseña actual,
/// el cambio de contraseña, la revocación de refresh tokens anteriores,
/// la generación de nuevos tokens y el envío del email de confirmación.
/// </summary>
[Trait("Category", "Application")]
[Trait("Feature", "Auth")]
[Trait("Component", "ChangePasswordCommandHandler")]
public class ChangePasswordCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ChangePasswordCommandHandler _handler;

    // La fecha del mock debe ser suficientemente futura para que AddDays(7) siga siendo
    // mayor que DateTime.UtcNow real. Se usa AddYears(1) para garantizarlo.
    private readonly DateTime _fixedNow = DateTime.UtcNow.AddYears(1);

    public ChangePasswordCommandHandlerTests()
    {
        _dbContext = DbContextFactory.CreateInMemoryContext();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _tokenService = Substitute.For<ITokenService>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _emailService = Substitute.For<IEmailService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        // Configuración por defecto de los mocks.
        // UtcNow usa Returns con función para garantizar evaluación en tiempo de llamada.
        _dateTimeProvider.UtcNow.Returns(_ => _fixedNow);
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed_new_password");
        _tokenService.GenerateAccessToken(Arg.Any<Domain.Entities.User>()).Returns("new-access-token");
        _tokenService.GenerateRefreshToken().Returns("new-refresh-token");
        _tokenService.GetRefreshTokenExpirationInDays().Returns(7);
        _tokenService.GetAccessTokenExpirationInSeconds().Returns(86400);

        var emailSettings = Options.Create(new EmailSettings
        {
            FrontendUrl = "http://localhost:4200",
            SenderName = "Work Scholarship System",
            SenderEmail = "noreply@worksholarship.dev"
        });

        _handler = new ChangePasswordCommandHandler(
            _dbContext,
            _passwordHasher,
            _tokenService,
            _currentUserService,
            _emailService,
            _dateTimeProvider,
            emailSettings);
    }

    public void Dispose() => _dbContext.Dispose();

    // =====================================================================
    // Autenticación — usuario no encontrado
    // =====================================================================

    /// <summary>
    /// Verifica que el handler retorna UNAUTHORIZED cuando ICurrentUserService
    /// no tiene un UserId (usuario no autenticado o token JWT inválido).
    /// </summary>
    [Fact]
    public async Task Handle_WhenCurrentUserServiceReturnsNullUserId_ReturnsUnauthorizedFailure()
    {
        // Arrange
        _currentUserService.UserId.Returns((Guid?)null);
        var command = new ChangePasswordCommand("CurrentPass1", "NewSecurePass1", "NewSecurePass1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AuthErrorCodes.UNAUTHORIZED);
    }

    /// <summary>
    /// Verifica que el handler retorna UNAUTHORIZED cuando el UserId existe en el claim
    /// pero no corresponde a ningún usuario en la BD (usuario eliminado o Guid incorrecto).
    /// </summary>
    [Fact]
    public async Task Handle_WhenUserNotFoundInDatabase_ReturnsUnauthorizedFailure()
    {
        // Arrange — UserId válido pero sin usuario correspondiente en la BD
        _currentUserService.UserId.Returns(Guid.NewGuid());
        var command = new ChangePasswordCommand("CurrentPass1", "NewSecurePass1", "NewSecurePass1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AuthErrorCodes.UNAUTHORIZED);
    }

    // =====================================================================
    // Validación de contraseña actual
    // =====================================================================

    /// <summary>
    /// Verifica que el handler retorna INVALID_CURRENT_PASSWORD cuando el usuario
    /// es una cuenta de Google pura (sin hash de contraseña local).
    /// Los usuarios de Google deben cambiar contraseña desde Google, no desde la app.
    /// </summary>
    [Fact]
    public async Task Handle_WhenUserHasNoLocalPassword_ReturnsInvalidCurrentPasswordFailure()
    {
        // Arrange — cuenta de Google sin contraseña local
        var googleUser = Domain.Entities.User.CreateFromGoogle(
            "google@gmail.com", "Google", "User", "google-id-123", null, "system");

        _dbContext.Users.Add(googleUser);
        await _dbContext.SaveChangesAsync();

        // Configurar el mock para que el UserId coincida con el usuario creado
        _currentUserService.UserId.Returns(googleUser.Id);

        var command = new ChangePasswordCommand("any-password", "NewSecurePass1", "NewSecurePass1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AuthErrorCodes.INVALID_CURRENT_PASSWORD);
    }

    /// <summary>
    /// Verifica que el handler retorna INVALID_CURRENT_PASSWORD cuando la contraseña
    /// actual proporcionada no coincide con el hash almacenado en la BD.
    /// </summary>
    [Fact]
    public async Task Handle_WhenCurrentPasswordIsWrong_ReturnsInvalidCurrentPasswordFailure()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("user@univ.edu")
            .WithPasswordHash("stored_hash")
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _currentUserService.UserId.Returns(user.Id);
        _passwordHasher.Verify("wrong-password", "stored_hash").Returns(false);

        var command = new ChangePasswordCommand("wrong-password", "NewSecurePass1", "NewSecurePass1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AuthErrorCodes.INVALID_CURRENT_PASSWORD);
    }

    // =====================================================================
    // Happy path — cambio exitoso
    // =====================================================================

    /// <summary>
    /// Verifica que con credenciales válidas, el handler retorna Result.Success()
    /// con un TokenResponse que contiene el nuevo access token generado.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsSuccessWithNewAccessToken()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("user@univ.edu")
            .WithPasswordHash("stored_hash")
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _currentUserService.UserId.Returns(user.Id);
        _passwordHasher.Verify("CurrentPass1", "stored_hash").Returns(true);

        var command = new ChangePasswordCommand("CurrentPass1", "NewSecurePass1", "NewSecurePass1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("new-access-token");
        result.Value.TokenType.Should().Be("Bearer");
    }

    /// <summary>
    /// Verifica que tras el cambio exitoso, la nueva contraseña está hasheada
    /// y persiste correctamente en la BD. Nunca debe guardarse en texto plano.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidCredentials_PersistsNewHashedPassword()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("user@univ.edu")
            .WithPasswordHash("stored_hash")
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _currentUserService.UserId.Returns(user.Id);
        _passwordHasher.Verify("CurrentPass1", "stored_hash").Returns(true);

        var command = new ChangePasswordCommand("CurrentPass1", "NewSecurePass1", "NewSecurePass1");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedUser = await _dbContext.Users.FirstAsync(u => u.Email == "user@univ.edu");
        updatedUser.PasswordHash.Should().Be("hashed_new_password");
        _passwordHasher.Received(1).Hash("NewSecurePass1");
    }

    /// <summary>
    /// Verifica que tras el cambio exitoso, se revoca el refresh token anterior y se
    /// agrega uno nuevo, efectivamente cerrando sesiones en otros dispositivos.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidCredentials_RevokesOldRefreshTokensAndAddsNew()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("user@univ.edu")
            .WithPasswordHash("stored_hash")
            .Build();
        user.AddRefreshToken("old-refresh-token", _fixedNow.AddDays(7));
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _currentUserService.UserId.Returns(user.Id);
        _passwordHasher.Verify("CurrentPass1", "stored_hash").Returns(true);

        var command = new ChangePasswordCommand("CurrentPass1", "NewSecurePass1", "NewSecurePass1");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — token antiguo revocado, token nuevo creado
        var tokens = await _dbContext.RefreshTokens.ToListAsync();
        var oldToken = tokens.First(t => t.Token == "old-refresh-token");
        var newToken = tokens.First(t => t.Token == "new-refresh-token");

        oldToken.IsRevoked.Should().BeTrue();
        newToken.IsRevoked.Should().BeFalse();
    }

    /// <summary>
    /// Verifica que el handler envía exactamente un email de confirmación
    /// de cambio de contraseña al email del usuario.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidCredentials_SendsConfirmationEmail()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("user@univ.edu")
            .WithPasswordHash("stored_hash")
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _currentUserService.UserId.Returns(user.Id);
        _passwordHasher.Verify("CurrentPass1", "stored_hash").Returns(true);

        var command = new ChangePasswordCommand("CurrentPass1", "NewSecurePass1", "NewSecurePass1");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _emailService.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.To == "user@univ.edu"),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifica que el TokenResponse retornado incluye el nuevo refresh token
    /// generado y los metadatos de expiración correctos.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsNewRefreshTokenInResponse()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("user@univ.edu")
            .WithPasswordHash("stored_hash")
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _currentUserService.UserId.Returns(user.Id);
        _passwordHasher.Verify("CurrentPass1", "stored_hash").Returns(true);

        var command = new ChangePasswordCommand("CurrentPass1", "NewSecurePass1", "NewSecurePass1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value.RefreshTokenValue.Should().Be("new-refresh-token");
        result.Value.ExpiresIn.Should().Be(86400);
        result.Value.RefreshTokenExpirationDays.Should().Be(7);
    }
}
