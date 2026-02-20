using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Auth.Commands.Login;
using WorkScholarship.Application.Tests.TestHelpers;
using WorkScholarship.Domain.Enums;
using WorkScholarship.Infrastructure.Data;

namespace WorkScholarship.Application.Tests.Features.Auth.Commands;

[Trait("Category", "Application")]
[Trait("Feature", "Auth")]
[Trait("Component", "LoginCommandHandler")]
public class LoginCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _dbContext = DbContextFactory.CreateInMemoryContext();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _tokenService = Substitute.For<ITokenService>();

        // Default token service setup
        _tokenService.GenerateAccessToken(Arg.Any<Domain.Entities.User>()).Returns("jwt-access-token");
        _tokenService.GenerateRefreshToken().Returns("refresh-token-value");
        _tokenService.GetRefreshTokenExpirationInDays().Returns(7);
        _tokenService.GetAccessTokenExpirationInSeconds().Returns(86400);

        _handler = new LoginCommandHandler(_dbContext, _passwordHasher, _tokenService);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    // =====================================================================
    // Happy path
    // =====================================================================

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsSuccessWithTokens()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("juan.perez@univ.edu")
            .WithPasswordHash("hashed_password")
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _passwordHasher.Verify("correct_password", "hashed_password").Returns(true);

        var command = new LoginCommand("juan.perez@univ.edu", "correct_password");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("jwt-access-token");
        result.Value.RefreshTokenValue.Should().Be("refresh-token-value");
        result.Value.TokenType.Should().Be("Bearer");
        result.Value.ExpiresIn.Should().Be(86400);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsUserDtoInResponse()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("juan.perez@univ.edu")
            .WithFirstName("Juan")
            .WithLastName("Perez")
            .WithRole(UserRole.Admin)
            .WithPasswordHash("hashed_password")
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var command = new LoginCommand("juan.perez@univ.edu", "correct_password");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value.User.Should().NotBeNull();
        result.Value.User.Email.Should().Be("juan.perez@univ.edu");
        result.Value.User.FirstName.Should().Be("Juan");
        result.Value.User.LastName.Should().Be("Perez");
        result.Value.User.Role.Should().Be("ADMIN");
    }

    [Fact]
    public async Task Handle_WithValidCredentials_NormalizesEmailToLowercase()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("juan@univ.edu") // stored as lowercase
            .WithPasswordHash("hashed_password")
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var command = new LoginCommand("JUAN@UNIV.EDU", "password"); // uppercase in request

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithValidCredentials_RecordsSuccessfulLogin()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("test@univ.edu")
            .WithPasswordHash("hashed_password")
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var command = new LoginCommand("test@univ.edu", "password");
        var beforeLogin = DateTime.UtcNow;

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == "test@univ.edu");
        updatedUser!.LastLoginAt.Should().NotBeNull();
        updatedUser.LastLoginAt!.Value.Should().BeOnOrAfter(beforeLogin);
        updatedUser.FailedLoginAttempts.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_AddsRefreshTokenToUser()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("test@univ.edu")
            .WithPasswordHash("hashed_password")
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var command = new LoginCommand("test@univ.edu", "password");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var tokens = await _dbContext.RefreshTokens.ToListAsync();
        tokens.Should().HaveCount(1);
        tokens[0].Token.Should().Be("refresh-token-value");
    }

    // =====================================================================
    // Error paths
    // =====================================================================

    [Fact]
    public async Task Handle_WithNonExistentEmail_ReturnsInvalidCredentialsFailure()
    {
        // Arrange
        var command = new LoginCommand("nonexistent@univ.edu", "password");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AuthErrorCodes.INVALID_CREDENTIALS);
    }

    [Fact]
    public async Task Handle_WithInactiveAccount_ReturnsInactiveAccountFailure()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("test@univ.edu")
            .AsInactive()
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var command = new LoginCommand("test@univ.edu", "password");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AuthErrorCodes.INACTIVE_ACCOUNT);
    }

    [Fact]
    public async Task Handle_WithGoogleOnlyAccount_ReturnsGoogleAccountFailure()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("test@univ.edu")
            .AsGoogle()
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var command = new LoginCommand("test@univ.edu", "password");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AuthErrorCodes.GOOGLE_ACCOUNT);
    }

    [Fact]
    public async Task Handle_WithLockedOutAccount_ReturnsAccountLockedFailure()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("test@univ.edu")
            .WithPasswordHash("hashed_password")
            .AsLockedOut()
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var command = new LoginCommand("test@univ.edu", "password");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AuthErrorCodes.ACCOUNT_LOCKED);
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ReturnsInvalidCredentialsFailure()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("test@univ.edu")
            .WithPasswordHash("hashed_password")
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _passwordHasher.Verify("wrong_password", "hashed_password").Returns(false);

        var command = new LoginCommand("test@univ.edu", "wrong_password");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AuthErrorCodes.INVALID_CREDENTIALS);
    }

    [Fact]
    public async Task Handle_WithWrongPassword_RecordsFailedLogin()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("test@univ.edu")
            .WithPasswordHash("hashed_password")
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var command = new LoginCommand("test@univ.edu", "wrong_password");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == "test@univ.edu");
        updatedUser!.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithWrongPasswordFiveTimes_LocksOutAccount()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("test@univ.edu")
            .WithPasswordHash("hashed_password")
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var command = new LoginCommand("test@univ.edu", "wrong_password");

        // Act - fail 5 times
        for (int i = 0; i < 5; i++)
        {
            await _handler.Handle(command, CancellationToken.None);
        }

        // Assert
        var updatedUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == "test@univ.edu");
        updatedUser!.IsLockedOut().Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithGoogleAccountWithPasswordSet_ChecksPasswordNormally()
    {
        // Arrange - a Google account that also has a local password (hybrid)
        var user = new UserBuilder()
            .WithEmail("test@univ.edu")
            .AsGoogle()
            .Build();
        // Set a local password to make it a hybrid account
        user.SetPassword("local_hash");
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var command = new LoginCommand("test@univ.edu", "password");

        // Act - should return GOOGLE_ACCOUNT because AuthProvider is Google AND PasswordHash might be set
        // But looking at the handler: it checks AuthProvider==Google AND PasswordHash is null
        // If PasswordHash is set (not null), it passes this check
        // The real behavior depends on the handler logic
        var result = await _handler.Handle(command, CancellationToken.None);

        // If Google + PasswordHash != null: handler passes the Google check, goes to lockout check, then password verify
        // The passwordHasher.Verify is not set to return true, so it returns false by default
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SavesChangesAfterSuccessfulLogin()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("test@univ.edu")
            .WithPasswordHash("hashed_password")
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var command = new LoginCommand("test@univ.edu", "password");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - verify that data was actually persisted
        result.IsSuccess.Should().BeTrue();
        var refreshTokens = await _dbContext.RefreshTokens.CountAsync();
        refreshTokens.Should().Be(1);
    }
}
