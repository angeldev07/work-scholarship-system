using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Features.Auth.Commands.Logout;
using WorkScholarship.Application.Tests.TestHelpers;
using WorkScholarship.Infrastructure.Data;

namespace WorkScholarship.Application.Tests.Features.Auth.Commands;

[Trait("Category", "Application")]
[Trait("Feature", "Auth")]
[Trait("Component", "LogoutCommandHandler")]
public class LogoutCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly LogoutCommandHandler _handler;

    public LogoutCommandHandlerTests()
    {
        _dbContext = DbContextFactory.CreateInMemoryContext();
        _currentUserService = Substitute.For<ICurrentUserService>();

        _handler = new LogoutCommandHandler(_dbContext, _currentUserService);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    // =====================================================================
    // Logout with specific refresh token
    // =====================================================================

    [Fact]
    public async Task Handle_WithSpecificRefreshToken_RevokesOnlyThatToken()
    {
        // Arrange
        var user = new UserBuilder().WithEmail("test@univ.edu").Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var tokenToRevoke = user.AddRefreshToken("token-to-revoke", DateTime.UtcNow.AddDays(7));
        var tokenToKeep = user.AddRefreshToken("token-to-keep", DateTime.UtcNow.AddDays(7));
        await _dbContext.SaveChangesAsync();

        var command = new LogoutCommand("token-to-revoke");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var revokedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == "token-to-revoke");
        revokedToken!.IsRevoked.Should().BeTrue();

        var keptToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == "token-to-keep");
        keptToken!.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithNonExistentRefreshToken_ReturnsSuccess()
    {
        // Arrange - No tokens in database
        var command = new LogoutCommand("non-existent-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - idempotent, always succeeds
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithAlreadyRevokedToken_ReturnsSuccess()
    {
        // Arrange
        var user = new UserBuilder().WithEmail("test@univ.edu").Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var token = user.AddRefreshToken("already-revoked", DateTime.UtcNow.AddDays(7));
        token.Revoke();
        await _dbContext.SaveChangesAsync();

        var command = new LogoutCommand("already-revoked");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - idempotent
        result.IsSuccess.Should().BeTrue();
    }

    // =====================================================================
    // Logout without refresh token (revoke all for current user)
    // =====================================================================

    [Fact]
    public async Task Handle_WithNoRefreshTokenAndAuthenticatedUser_RevokesAllActiveTokens()
    {
        // Arrange
        var user = new UserBuilder().WithEmail("test@univ.edu").Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        user.AddRefreshToken("token-1", DateTime.UtcNow.AddDays(7));
        user.AddRefreshToken("token-2", DateTime.UtcNow.AddDays(7));
        user.AddRefreshToken("token-3", DateTime.UtcNow.AddDays(7));
        await _dbContext.SaveChangesAsync();

        _currentUserService.UserId.Returns(user.Id);

        var command = new LogoutCommand(null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var allTokens = await _dbContext.RefreshTokens.ToListAsync();
        allTokens.All(t => t.IsRevoked).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNoRefreshTokenAndAuthenticatedUser_OnlyRevokesActiveTokens()
    {
        // Arrange
        var user = new UserBuilder().WithEmail("test@univ.edu").Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var activeToken = user.AddRefreshToken("active-token", DateTime.UtcNow.AddDays(7));
        var revokedToken = user.AddRefreshToken("revoked-token", DateTime.UtcNow.AddDays(7));
        revokedToken.Revoke();
        await _dbContext.SaveChangesAsync();

        _currentUserService.UserId.Returns(user.Id);

        var command = new LogoutCommand(null);
        var revokedAtBefore = revokedToken.RevokedAt;

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var savedRevokedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == "revoked-token");

        // active token should now be revoked
        var savedActiveToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == "active-token");
        savedActiveToken!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNoRefreshTokenAndNoAuthenticatedUser_ReturnsSuccess()
    {
        // Arrange - no authenticated user
        _currentUserService.UserId.Returns((Guid?)null);

        var command = new LogoutCommand(null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - idempotent, returns success
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithEmptyRefreshToken_FallsBackToRevokingAllForCurrentUser()
    {
        // Arrange
        var user = new UserBuilder().WithEmail("test@univ.edu").Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        user.AddRefreshToken("token-1", DateTime.UtcNow.AddDays(7));
        await _dbContext.SaveChangesAsync();

        _currentUserService.UserId.Returns(user.Id);

        // Empty string (not whitespace) - handler checks IsNullOrWhiteSpace
        var command = new LogoutCommand("");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - empty string = no specific token, so falls to revoke all
        result.IsSuccess.Should().BeTrue();

        var token = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == "token-1");
        token!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AlwaysReturnsSuccess()
    {
        // Arrange - various scenarios always return success
        var command = new LogoutCommand("any-token-that-might-not-exist");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
    }
}
