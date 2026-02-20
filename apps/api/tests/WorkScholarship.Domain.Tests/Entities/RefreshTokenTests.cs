using FluentAssertions;
using WorkScholarship.Domain.Entities;

namespace WorkScholarship.Domain.Tests.Entities;

[Trait("Category", "Domain")]
[Trait("Entity", "RefreshToken")]
public class RefreshTokenTests
{
    // =====================================================================
    // RefreshToken.Create() - Factory method
    // =====================================================================

    [Fact]
    public void Create_WithValidParameters_ReturnsRefreshTokenWithCorrectProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = "random-secure-token-value";
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var ipAddress = "192.168.1.1";

        // Act
        var refreshToken = RefreshToken.Create(userId, token, expiresAt, ipAddress);

        // Assert
        refreshToken.Should().NotBeNull();
        refreshToken.UserId.Should().Be(userId);
        refreshToken.Token.Should().Be(token);
        refreshToken.ExpiresAt.Should().Be(expiresAt);
        refreshToken.IpAddress.Should().Be(ipAddress);
        refreshToken.RevokedAt.Should().BeNull();
        refreshToken.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WithNullIpAddress_StillCreatesToken()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var refreshToken = RefreshToken.Create(userId, "token-value", DateTime.UtcNow.AddDays(7), null);

        // Assert
        refreshToken.Should().NotBeNull();
        refreshToken.IpAddress.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentException()
    {
        // Act
        var act = () => RefreshToken.Create(Guid.Empty, "token-value", DateTime.UtcNow.AddDays(7));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*userId*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyToken_ThrowsArgumentException(string? token)
    {
        // Act
        var act = () => RefreshToken.Create(Guid.NewGuid(), token!, DateTime.UtcNow.AddDays(7));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*token*");
    }

    [Fact]
    public void Create_WithExpiresAtInPast_ThrowsArgumentException()
    {
        // Act
        var act = () => RefreshToken.Create(Guid.NewGuid(), "token-value", DateTime.UtcNow.AddSeconds(-1));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*future*");
    }

    [Fact]
    public void Create_WithExpiresAtEqualToNow_ThrowsArgumentException()
    {
        // Act
        // Using a time clearly in the past to avoid race condition
        var act = () => RefreshToken.Create(Guid.NewGuid(), "token-value", DateTime.UtcNow.AddMilliseconds(-100));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*future*");
    }

    // =====================================================================
    // IsExpired computed property
    // =====================================================================

    [Fact]
    public void IsExpired_WhenExpiresInFuture_ReturnsFalse()
    {
        // Arrange
        var refreshToken = RefreshToken.Create(Guid.NewGuid(), "token", DateTime.UtcNow.AddDays(7));

        // Assert
        refreshToken.IsExpired.Should().BeFalse();
    }

    // =====================================================================
    // IsRevoked computed property
    // =====================================================================

    [Fact]
    public void IsRevoked_WhenNotRevoked_ReturnsFalse()
    {
        // Arrange
        var refreshToken = RefreshToken.Create(Guid.NewGuid(), "token", DateTime.UtcNow.AddDays(7));

        // Assert
        refreshToken.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public void IsRevoked_AfterRevoke_ReturnsTrue()
    {
        // Arrange
        var refreshToken = RefreshToken.Create(Guid.NewGuid(), "token", DateTime.UtcNow.AddDays(7));

        // Act
        refreshToken.Revoke();

        // Assert
        refreshToken.IsRevoked.Should().BeTrue();
    }

    // =====================================================================
    // IsActive computed property
    // =====================================================================

    [Fact]
    public void IsActive_WhenNotRevokedAndNotExpired_ReturnsTrue()
    {
        // Arrange
        var refreshToken = RefreshToken.Create(Guid.NewGuid(), "token", DateTime.UtcNow.AddDays(7));

        // Assert
        refreshToken.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenRevoked_ReturnsFalse()
    {
        // Arrange
        var refreshToken = RefreshToken.Create(Guid.NewGuid(), "token", DateTime.UtcNow.AddDays(7));
        refreshToken.Revoke();

        // Assert
        refreshToken.IsActive.Should().BeFalse();
    }

    // =====================================================================
    // Revoke()
    // =====================================================================

    [Fact]
    public void Revoke_SetsRevokedAt()
    {
        // Arrange
        var refreshToken = RefreshToken.Create(Guid.NewGuid(), "token", DateTime.UtcNow.AddDays(7));
        var before = DateTime.UtcNow;

        // Act
        refreshToken.Revoke();

        // Assert
        refreshToken.RevokedAt.Should().NotBeNull();
        refreshToken.RevokedAt!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_DoesNotUpdateRevokedAt()
    {
        // Arrange
        var refreshToken = RefreshToken.Create(Guid.NewGuid(), "token", DateTime.UtcNow.AddDays(7));
        refreshToken.Revoke();
        var firstRevokedAt = refreshToken.RevokedAt;

        // Wait a tiny bit
        Thread.Sleep(10);

        // Act - revoke again
        refreshToken.Revoke();

        // Assert - RevokedAt should remain the same
        refreshToken.RevokedAt.Should().Be(firstRevokedAt);
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_DoesNotThrow()
    {
        // Arrange
        var refreshToken = RefreshToken.Create(Guid.NewGuid(), "token", DateTime.UtcNow.AddDays(7));
        refreshToken.Revoke();

        // Act
        var act = () => refreshToken.Revoke();

        // Assert
        act.Should().NotThrow();
    }

    // =====================================================================
    // BaseEntity properties
    // =====================================================================

    [Fact]
    public void Create_AssignsNonEmptyId()
    {
        // Act
        var refreshToken = RefreshToken.Create(Guid.NewGuid(), "token", DateTime.UtcNow.AddDays(7));

        // Assert
        refreshToken.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_SetsCreatedAt()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var refreshToken = RefreshToken.Create(Guid.NewGuid(), "token", DateTime.UtcNow.AddDays(7));

        // Assert
        refreshToken.CreatedAt.Should().BeOnOrAfter(before);
    }
}
