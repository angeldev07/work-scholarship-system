using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Auth.Commands.ResetPassword;
using WorkScholarship.Application.Tests.TestHelpers;
using WorkScholarship.Infrastructure.Data;

namespace WorkScholarship.Application.Tests.Features.Auth.Commands;

/// <summary>
/// Tests unitarios para ResetPasswordCommandHandler.
/// Verifica la validación del token de reset, el cambio de contraseña,
/// la revocación de refresh tokens y la eliminación del token usado.
/// </summary>
[Trait("Category", "Application")]
[Trait("Feature", "Auth")]
[Trait("Component", "ResetPasswordCommandHandler")]
public class ResetPasswordCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ResetPasswordCommandHandler _handler;

    private const string ValidResetToken = "validresettoken1234567890abcdef1234567890abcdef1234567890abcdef12345678";
    private static readonly DateTime _validExpiry = DateTime.UtcNow.AddHours(1);

    public ResetPasswordCommandHandlerTests()
    {
        _dbContext = DbContextFactory.CreateInMemoryContext();
        _passwordHasher = Substitute.For<IPasswordHasher>();

        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed_new_password");

        _handler = new ResetPasswordCommandHandler(_dbContext, _passwordHasher);
    }

    public void Dispose() => _dbContext.Dispose();

    // =====================================================================
    // Validación del token
    // =====================================================================

    /// <summary>
    /// Verifica que el handler retorna INVALID_TOKEN cuando no existe ningún
    /// usuario con el token de reset proporcionado.
    /// </summary>
    [Fact]
    public async Task Handle_WithNonExistentToken_ReturnsInvalidTokenFailure()
    {
        // Arrange
        var command = new ResetPasswordCommand("nonexistent-token", "SecurePass1", "SecurePass1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AuthErrorCodes.INVALID_TOKEN);
    }

    /// <summary>
    /// Verifica que el handler retorna INVALID_TOKEN cuando el token existe
    /// pero su expiración ya pasó (token expirado).
    /// </summary>
    [Fact]
    public async Task Handle_WithExpiredToken_ReturnsInvalidTokenFailure()
    {
        // Arrange — token cuya expiración fue hace 1 segundo
        var user = new UserBuilder()
            .WithEmail("user@univ.edu")
            .Build();
        user.SetPasswordResetToken(ValidResetToken, DateTime.UtcNow.AddSeconds(-1));
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var command = new ResetPasswordCommand(ValidResetToken, "SecurePass1", "SecurePass1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AuthErrorCodes.INVALID_TOKEN);
    }

    // =====================================================================
    // Happy path — token válido y no expirado
    // =====================================================================

    /// <summary>
    /// Verifica que con un token válido, el handler retorna Result.Success()
    /// y persiste el nuevo hash de contraseña en la BD.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidToken_ReturnsSuccessAndUpdatesPasswordHash()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("user@univ.edu")
            .Build();
        user.SetPasswordResetToken(ValidResetToken, _validExpiry);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var command = new ResetPasswordCommand(ValidResetToken, "NewSecurePass1", "NewSecurePass1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var updatedUser = await _dbContext.Users.FirstAsync(u => u.Email == "user@univ.edu");
        updatedUser.PasswordHash.Should().Be("hashed_new_password");
    }

    /// <summary>
    /// Verifica que tras un reset exitoso, el token de reset es eliminado del usuario
    /// para que no pueda ser reutilizado (token de un solo uso).
    /// </summary>
    [Fact]
    public async Task Handle_WithValidToken_ClearsResetTokenAfterUse()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("user@univ.edu")
            .Build();
        user.SetPasswordResetToken(ValidResetToken, _validExpiry);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var command = new ResetPasswordCommand(ValidResetToken, "NewSecurePass1", "NewSecurePass1");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — el token debe ser nulo tras el reset
        var updatedUser = await _dbContext.Users.FirstAsync(u => u.Email == "user@univ.edu");
        updatedUser.PasswordResetToken.Should().BeNull();
        updatedUser.PasswordResetTokenExpiresAt.Should().BeNull();
    }

    /// <summary>
    /// Verifica que el handler llama a IPasswordHasher.Hash() con la nueva contraseña
    /// antes de persistirla, garantizando que nunca se almacena en texto plano.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidToken_HashesNewPasswordBeforeSaving()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("user@univ.edu")
            .Build();
        user.SetPasswordResetToken(ValidResetToken, _validExpiry);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var command = new ResetPasswordCommand(ValidResetToken, "NewSecurePass1", "NewSecurePass1");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _passwordHasher.Received(1).Hash("NewSecurePass1");
    }

    // =====================================================================
    // Seguridad — revocación de refresh tokens
    // =====================================================================

    /// <summary>
    /// Verifica que tras un reset exitoso, todos los refresh tokens activos del usuario
    /// son revocados, cerrando todas las sesiones activas en otros dispositivos.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidToken_RevokesAllActiveRefreshTokens()
    {
        // Arrange — usuario con un refresh token activo
        var user = new UserBuilder()
            .WithEmail("user@univ.edu")
            .Build();
        user.SetPasswordResetToken(ValidResetToken, _validExpiry);

        var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        user.AddRefreshToken("active-refresh-token", refreshTokenExpiry);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var command = new ResetPasswordCommand(ValidResetToken, "NewSecurePass1", "NewSecurePass1");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — el refresh token debe estar revocado
        var refreshToken = await _dbContext.RefreshTokens.FirstAsync();
        refreshToken.IsRevoked.Should().BeTrue();
    }

    // =====================================================================
    // Reutilización del token (token ya usado)
    // =====================================================================

    /// <summary>
    /// Verifica que usar el mismo token de reset dos veces falla en la segunda llamada,
    /// dado que SetPassword() limpia el token tras el primer uso.
    /// </summary>
    [Fact]
    public async Task Handle_WithAlreadyUsedToken_ReturnsInvalidTokenFailureOnSecondCall()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("user@univ.edu")
            .Build();
        user.SetPasswordResetToken(ValidResetToken, _validExpiry);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var command = new ResetPasswordCommand(ValidResetToken, "NewSecurePass1", "NewSecurePass1");

        // Act — primer uso (exitoso)
        await _handler.Handle(command, CancellationToken.None);

        // Act — segundo uso con el mismo token (debe fallar)
        var secondResult = await _handler.Handle(command, CancellationToken.None);

        // Assert
        secondResult.IsFailure.Should().BeTrue();
        secondResult.Error!.Code.Should().Be(AuthErrorCodes.INVALID_TOKEN);
    }
}
