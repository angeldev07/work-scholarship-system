using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Auth.Commands.RefreshToken;
using WorkScholarship.Application.Tests.TestHelpers;
using WorkScholarship.Infrastructure.Data;

namespace WorkScholarship.Application.Tests.Features.Auth.Commands;

[Trait("Category", "Application")]
[Trait("Feature", "Auth")]
[Trait("Component", "RefreshTokenCommandHandler")]
public class RefreshTokenCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _dbContext = DbContextFactory.CreateInMemoryContext();
        _tokenService = Substitute.For<ITokenService>();

        _tokenService.GenerateAccessToken(Arg.Any<Domain.Entities.User>()).Returns("new-access-token");
        _tokenService.GenerateRefreshToken().Returns("new-refresh-token");
        _tokenService.GetRefreshTokenExpirationInDays().Returns(7);
        _tokenService.GetAccessTokenExpirationInSeconds().Returns(86400);

        _handler = new RefreshTokenCommandHandler(_dbContext, _tokenService);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    // =====================================================================
    // Happy path - Token rotation
    // =====================================================================

    [Fact]
    public async Task Handle_WithValidRefreshToken_ReturnsNewTokens()
    {
        // Arrange
        var user = new UserBuilder().WithEmail("test@univ.edu").Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var existingToken = user.AddRefreshToken("valid-refresh-token", DateTime.UtcNow.AddDays(7));
        await _dbContext.SaveChangesAsync();

        var command = new RefreshTokenCommand("valid-refresh-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("new-access-token");
        result.Value.RefreshTokenValue.Should().Be("new-refresh-token");
        result.Value.TokenType.Should().Be("Bearer");
        result.Value.ExpiresIn.Should().Be(86400);
    }

    [Fact]
    public async Task Handle_WithValidRefreshToken_RevokesOldToken()
    {
        // Arrange
        var user = new UserBuilder().WithEmail("test@univ.edu").Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        user.AddRefreshToken("valid-refresh-token", DateTime.UtcNow.AddDays(7));
        await _dbContext.SaveChangesAsync();

        var command = new RefreshTokenCommand("valid-refresh-token");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var oldToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == "valid-refresh-token");
        oldToken!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithValidRefreshToken_CreatesNewRefreshToken()
    {
        // Arrange
        var user = new UserBuilder().WithEmail("test@univ.edu").Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        user.AddRefreshToken("valid-refresh-token", DateTime.UtcNow.AddDays(7));
        await _dbContext.SaveChangesAsync();

        var command = new RefreshTokenCommand("valid-refresh-token");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var newToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == "new-refresh-token");
        newToken.Should().NotBeNull();
        newToken!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithValidRefreshToken_RefreshTokenExpirationDaysIsCorrect()
    {
        // Arrange
        var user = new UserBuilder().WithEmail("test@univ.edu").Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        user.AddRefreshToken("valid-token", DateTime.UtcNow.AddDays(7));
        await _dbContext.SaveChangesAsync();

        var command = new RefreshTokenCommand("valid-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value.RefreshTokenExpirationDays.Should().Be(7);
    }

    // =====================================================================
    // Error paths
    // =====================================================================

    [Fact]
    public async Task Handle_WithNullOrEmptyToken_ReturnsInvalidRefreshTokenFailure()
    {
        // Arrange
        var command = new RefreshTokenCommand("");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AuthErrorCodes.INVALID_REFRESH_TOKEN);
    }

    [Fact]
    public async Task Handle_WithWhitespaceToken_ReturnsInvalidRefreshTokenFailure()
    {
        // Arrange
        var command = new RefreshTokenCommand("   ");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AuthErrorCodes.INVALID_REFRESH_TOKEN);
    }

    [Fact]
    public async Task Handle_WithNonExistentToken_ReturnsInvalidRefreshTokenFailure()
    {
        // Arrange
        var command = new RefreshTokenCommand("non-existent-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AuthErrorCodes.INVALID_REFRESH_TOKEN);
    }

    [Fact]
    public async Task Handle_WithRevokedToken_ReturnsInvalidRefreshTokenFailure()
    {
        // Arrange
        var user = new UserBuilder().WithEmail("test@univ.edu").Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var token = user.AddRefreshToken("revoked-token", DateTime.UtcNow.AddDays(7));
        token.Revoke();
        await _dbContext.SaveChangesAsync();

        var command = new RefreshTokenCommand("revoked-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AuthErrorCodes.INVALID_REFRESH_TOKEN);
    }

    [Fact]
    public async Task Handle_WithInactiveUserAccount_ReturnsSessionExpiredFailure()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("test@univ.edu")
            .AsInactive()
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        user.AddRefreshToken("active-token", DateTime.UtcNow.AddDays(7));
        await _dbContext.SaveChangesAsync();

        var command = new RefreshTokenCommand("active-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AuthErrorCodes.SESSION_EXPIRED);
    }

    [Fact]
    public async Task Handle_WithValidToken_SavesChangesToDatabase()
    {
        // Arrange
        var user = new UserBuilder().WithEmail("test@univ.edu").Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        user.AddRefreshToken("old-token", DateTime.UtcNow.AddDays(7));
        await _dbContext.SaveChangesAsync();

        var command = new RefreshTokenCommand("old-token");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - old token revoked, new token created
        var tokenCount = await _dbContext.RefreshTokens.CountAsync();
        tokenCount.Should().Be(2);

        var newToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == "new-refresh-token");
        newToken.Should().NotBeNull();
    }
}
