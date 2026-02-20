using FluentAssertions;
using WorkScholarship.Domain.Entities;
using WorkScholarship.Domain.Enums;

namespace WorkScholarship.Domain.Tests.Entities;

[Trait("Category", "Domain")]
[Trait("Entity", "User")]
public class UserTests
{
    // =====================================================================
    // User.Create() - Factory method for local authentication
    // =====================================================================

    [Fact]
    public void Create_WithValidParameters_ReturnsUserWithCorrectProperties()
    {
        // Arrange
        var email = "juan.perez@univ.edu";
        var firstName = "Juan";
        var lastName = "Perez";
        var passwordHash = "hashed_password_123";
        var role = UserRole.Beca;
        var createdBy = "admin";

        // Act
        var user = User.Create(email, firstName, lastName, passwordHash, role, createdBy);

        // Assert
        user.Should().NotBeNull();
        user.Email.Should().Be("juan.perez@univ.edu");
        user.FirstName.Should().Be("Juan");
        user.LastName.Should().Be("Perez");
        user.PasswordHash.Should().Be(passwordHash);
        user.Role.Should().Be(UserRole.Beca);
        user.AuthProvider.Should().Be(AuthProvider.Local);
        user.IsActive.Should().BeTrue();
        user.FailedLoginAttempts.Should().Be(0);
        user.CreatedBy.Should().Be("admin");
        user.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_NormalizesEmailToLowercase()
    {
        // Arrange
        var email = "JUAN.PEREZ@UNIV.EDU";

        // Act
        var user = User.Create(email, "Juan", "Perez", "hash", UserRole.Beca, "admin");

        // Assert
        user.Email.Should().Be("juan.perez@univ.edu");
    }

    [Fact]
    public void Create_TrimsWhitespaceFromNames()
    {
        // Act
        var user = User.Create("test@univ.edu", "  Juan  ", "  Perez  ", "hash", UserRole.Beca, "admin");

        // Assert
        user.FirstName.Should().Be("Juan");
        user.LastName.Should().Be("Perez");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyEmail_ThrowsArgumentException(string? email)
    {
        // Act
        var act = () => User.Create(email!, "Juan", "Perez", "hash", UserRole.Beca, "admin");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*email*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyFirstName_ThrowsArgumentException(string? firstName)
    {
        // Act
        var act = () => User.Create("test@univ.edu", firstName!, "Perez", "hash", UserRole.Beca, "admin");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*firstName*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyLastName_ThrowsArgumentException(string? lastName)
    {
        // Act
        var act = () => User.Create("test@univ.edu", "Juan", lastName!, "hash", UserRole.Beca, "admin");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*lastName*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyPasswordHash_ThrowsArgumentException(string? passwordHash)
    {
        // Act
        var act = () => User.Create("test@univ.edu", "Juan", "Perez", passwordHash!, UserRole.Beca, "admin");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*passwordHash*");
    }

    [Fact]
    public void Create_SetsCorrectAuthProvider()
    {
        // Act
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Admin, "system");

        // Assert
        user.AuthProvider.Should().Be(AuthProvider.Local);
    }

    [Fact]
    public void Create_WithNullPasswordHash_DoesNotSetGoogleId()
    {
        // Act
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");

        // Assert
        user.GoogleId.Should().BeNull();
    }

    // =====================================================================
    // User.CreateFromGoogle() - Factory method for Google OAuth
    // =====================================================================

    [Fact]
    public void CreateFromGoogle_WithValidParameters_ReturnsUserWithCorrectProperties()
    {
        // Arrange
        var email = "juan.perez@univ.edu";
        var firstName = "Juan";
        var lastName = "Perez";
        var googleId = "google-id-123";
        var photoUrl = "https://photo.url/pic.jpg";

        // Act
        var user = User.CreateFromGoogle(email, firstName, lastName, googleId, photoUrl, "system");

        // Assert
        user.Should().NotBeNull();
        user.Email.Should().Be("juan.perez@univ.edu");
        user.FirstName.Should().Be("Juan");
        user.LastName.Should().Be("Perez");
        user.PasswordHash.Should().BeNull();
        user.GoogleId.Should().Be(googleId);
        user.PhotoUrl.Should().Be(photoUrl);
        user.Role.Should().Be(UserRole.None);
        user.AuthProvider.Should().Be(AuthProvider.Google);
        user.IsActive.Should().BeTrue();
        user.FailedLoginAttempts.Should().Be(0);
    }

    [Fact]
    public void CreateFromGoogle_NormalizesEmailToLowercase()
    {
        // Act
        var user = User.CreateFromGoogle("JUAN@UNIV.EDU", "Juan", "Perez", null, null, "system");

        // Assert
        user.Email.Should().Be("juan@univ.edu");
    }

    [Fact]
    public void CreateFromGoogle_WithNullGoogleIdAndPhotoUrl_StillCreatesUser()
    {
        // Act
        var user = User.CreateFromGoogle("test@univ.edu", "Juan", "Perez", null, null, "system");

        // Assert
        user.Should().NotBeNull();
        user.GoogleId.Should().BeNull();
        user.PhotoUrl.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateFromGoogle_WithNullOrEmptyEmail_ThrowsArgumentException(string? email)
    {
        // Act
        var act = () => User.CreateFromGoogle(email!, "Juan", "Perez", null, null, "system");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*email*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateFromGoogle_WithNullOrEmptyFirstName_ThrowsArgumentException(string? firstName)
    {
        // Act
        var act = () => User.CreateFromGoogle("test@univ.edu", firstName!, "Perez", null, null, "system");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*firstName*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateFromGoogle_WithNullOrEmptyLastName_ThrowsArgumentException(string? lastName)
    {
        // Act
        var act = () => User.CreateFromGoogle("test@univ.edu", "Juan", lastName!, null, null, "system");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*lastName*");
    }

    // =====================================================================
    // FullName computed property
    // =====================================================================

    [Fact]
    public void FullName_ReturnsCombinedFirstAndLastName()
    {
        // Act
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");

        // Assert
        user.FullName.Should().Be("Juan Perez");
    }

    // =====================================================================
    // SetPassword()
    // =====================================================================

    [Fact]
    public void SetPassword_WithValidHash_UpdatesPasswordHash()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "old_hash", UserRole.Beca, "admin");
        var newHash = "new_hashed_password";

        // Act
        user.SetPassword(newHash);

        // Assert
        user.PasswordHash.Should().Be(newHash);
    }

    [Fact]
    public void SetPassword_ClearsPasswordResetToken()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");
        user.SetPasswordResetToken("reset-token", DateTime.UtcNow.AddHours(1));

        // Act
        user.SetPassword("new_hash");

        // Assert
        user.PasswordResetToken.Should().BeNull();
        user.PasswordResetTokenExpiresAt.Should().BeNull();
    }

    [Fact]
    public void SetPassword_UpdatesUpdatedAt()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");
        var before = DateTime.UtcNow;

        // Act
        user.SetPassword("new_hash");

        // Assert
        user.UpdatedAt.Should().NotBeNull();
        user.UpdatedAt!.Value.Should().BeOnOrAfter(before);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetPassword_WithNullOrEmptyHash_ThrowsArgumentException(string? passwordHash)
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");

        // Act
        var act = () => user.SetPassword(passwordHash!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*passwordHash*");
    }

    // =====================================================================
    // ChangeRole()
    // =====================================================================

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.Supervisor)]
    [InlineData(UserRole.Beca)]
    public void ChangeRole_WithValidRole_UpdatesRole(UserRole newRole)
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");

        // Act
        user.ChangeRole(newRole, "admin");

        // Assert
        user.Role.Should().Be(newRole);
    }

    [Fact]
    public void ChangeRole_UpdatesUpdatedByAndUpdatedAt()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");
        var before = DateTime.UtcNow;

        // Act
        user.ChangeRole(UserRole.Supervisor, "superadmin");

        // Assert
        user.UpdatedBy.Should().Be("superadmin");
        user.UpdatedAt.Should().NotBeNull();
        user.UpdatedAt!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void ChangeRole_WithNoneRole_ThrowsArgumentException()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");

        // Act
        var act = () => user.ChangeRole(UserRole.None, "admin");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*None role*");
    }

    // =====================================================================
    // RecordSuccessfulLogin()
    // =====================================================================

    [Fact]
    public void RecordSuccessfulLogin_UpdatesLastLoginAt()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");
        var before = DateTime.UtcNow;

        // Act
        user.RecordSuccessfulLogin();

        // Assert
        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void RecordSuccessfulLogin_ResetsFailedLoginAttempts()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");
        user.RecordFailedLogin();
        user.RecordFailedLogin();

        // Act
        user.RecordSuccessfulLogin();

        // Assert
        user.FailedLoginAttempts.Should().Be(0);
    }

    [Fact]
    public void RecordSuccessfulLogin_ClearsLockout()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");
        // Trigger lockout by recording 5 failed attempts
        for (int i = 0; i < User.MAX_FAILED_LOGIN_ATTEMPTS; i++)
            user.RecordFailedLogin();

        user.IsLockedOut().Should().BeTrue();

        // Act
        user.RecordSuccessfulLogin();

        // Assert
        user.LockoutEndAt.Should().BeNull();
        user.IsLockedOut().Should().BeFalse();
    }

    // =====================================================================
    // RecordFailedLogin()
    // =====================================================================

    [Fact]
    public void RecordFailedLogin_IncrementsFailedLoginAttempts()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");

        // Act
        user.RecordFailedLogin();

        // Assert
        user.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public void RecordFailedLogin_WhenAtMaxAttempts_SetsLockout()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");
        var before = DateTime.UtcNow;

        // Act - reach exactly MAX_FAILED_LOGIN_ATTEMPTS
        for (int i = 0; i < User.MAX_FAILED_LOGIN_ATTEMPTS; i++)
            user.RecordFailedLogin();

        // Assert
        user.FailedLoginAttempts.Should().Be(User.MAX_FAILED_LOGIN_ATTEMPTS);
        user.LockoutEndAt.Should().NotBeNull();
        user.LockoutEndAt!.Value.Should().BeOnOrAfter(before.AddMinutes(User.LOCKOUT_DURATION_MINUTES));
    }

    [Fact]
    public void RecordFailedLogin_BelowMaxAttempts_DoesNotSetLockout()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");

        // Act - one less than max
        for (int i = 0; i < User.MAX_FAILED_LOGIN_ATTEMPTS - 1; i++)
            user.RecordFailedLogin();

        // Assert
        user.LockoutEndAt.Should().BeNull();
        user.IsLockedOut().Should().BeFalse();
    }

    [Fact]
    public void RecordFailedLogin_MaxAttemptIs5()
    {
        // Assert constant value
        User.MAX_FAILED_LOGIN_ATTEMPTS.Should().Be(5);
    }

    [Fact]
    public void RecordFailedLogin_LockoutDurationIs15Minutes()
    {
        // Assert constant value
        User.LOCKOUT_DURATION_MINUTES.Should().Be(15);
    }

    // =====================================================================
    // IsLockedOut()
    // =====================================================================

    [Fact]
    public void IsLockedOut_WhenNoLockout_ReturnsFalse()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");

        // Assert
        user.IsLockedOut().Should().BeFalse();
    }

    [Fact]
    public void IsLockedOut_WhenLockoutInFuture_ReturnsTrue()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");
        for (int i = 0; i < User.MAX_FAILED_LOGIN_ATTEMPTS; i++)
            user.RecordFailedLogin();

        // Assert
        user.IsLockedOut().Should().BeTrue();
    }

    // =====================================================================
    // LinkGoogleAccount()
    // =====================================================================

    [Fact]
    public void LinkGoogleAccount_WithValidGoogleId_SetsGoogleIdAndChangesProvider()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");

        // Act
        user.LinkGoogleAccount("google-id-123", "https://photo.url/pic.jpg");

        // Assert
        user.GoogleId.Should().Be("google-id-123");
        user.AuthProvider.Should().Be(AuthProvider.Google);
    }

    [Fact]
    public void LinkGoogleAccount_WithPhotoUrl_SetsPhotoUrl()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");

        // Act
        user.LinkGoogleAccount("google-id-123", "https://photo.url/pic.jpg");

        // Assert
        user.PhotoUrl.Should().Be("https://photo.url/pic.jpg");
    }

    [Fact]
    public void LinkGoogleAccount_WithNullPhotoUrl_DoesNotUpdatePhotoUrl()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");

        // Act
        user.LinkGoogleAccount("google-id-123", null);

        // Assert
        user.PhotoUrl.Should().BeNull();
    }

    [Fact]
    public void LinkGoogleAccount_WithEmptyPhotoUrl_DoesNotUpdatePhotoUrl()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");

        // Act
        user.LinkGoogleAccount("google-id-123", "   ");

        // Assert
        user.PhotoUrl.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void LinkGoogleAccount_WithNullOrEmptyGoogleId_ThrowsArgumentException(string? googleId)
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");

        // Act
        var act = () => user.LinkGoogleAccount(googleId!, null);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*googleId*");
    }

    // =====================================================================
    // SetPasswordResetToken()
    // =====================================================================

    [Fact]
    public void SetPasswordResetToken_WithValidToken_SetsTokenAndExpiry()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");
        var token = "reset-token-abc";
        var expiry = DateTime.UtcNow.AddHours(1);

        // Act
        user.SetPasswordResetToken(token, expiry);

        // Assert
        user.PasswordResetToken.Should().Be(token);
        user.PasswordResetTokenExpiresAt.Should().Be(expiry);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetPasswordResetToken_WithNullOrEmptyToken_ThrowsArgumentException(string? token)
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");

        // Act
        var act = () => user.SetPasswordResetToken(token!, DateTime.UtcNow.AddHours(1));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*token*");
    }

    // =====================================================================
    // IsPasswordResetTokenValid()
    // =====================================================================

    [Fact]
    public void IsPasswordResetTokenValid_WithMatchingNonExpiredToken_ReturnsTrue()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");
        user.SetPasswordResetToken("valid-token", DateTime.UtcNow.AddHours(1));

        // Assert
        user.IsPasswordResetTokenValid("valid-token").Should().BeTrue();
    }

    [Fact]
    public void IsPasswordResetTokenValid_WithWrongToken_ReturnsFalse()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");
        user.SetPasswordResetToken("correct-token", DateTime.UtcNow.AddHours(1));

        // Assert
        user.IsPasswordResetTokenValid("wrong-token").Should().BeFalse();
    }

    [Fact]
    public void IsPasswordResetTokenValid_WithExpiredToken_ReturnsFalse()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");
        user.SetPasswordResetToken("expired-token", DateTime.UtcNow.AddHours(-1));

        // Assert
        user.IsPasswordResetTokenValid("expired-token").Should().BeFalse();
    }

    [Fact]
    public void IsPasswordResetTokenValid_WhenNoTokenSet_ReturnsFalse()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");

        // Assert
        user.IsPasswordResetTokenValid("any-token").Should().BeFalse();
    }

    // =====================================================================
    // Deactivate() and Activate()
    // =====================================================================

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");

        // Act
        user.Deactivate("admin");

        // Assert
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_UpdatesUpdatedByAndUpdatedAt()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");
        var before = DateTime.UtcNow;

        // Act
        user.Deactivate("superadmin");

        // Assert
        user.UpdatedBy.Should().Be("superadmin");
        user.UpdatedAt.Should().NotBeNull();
        user.UpdatedAt!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Activate_SetsIsActiveToTrue()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");
        user.Deactivate("admin");
        user.IsActive.Should().BeFalse();

        // Act
        user.Activate("admin");

        // Assert
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Activate_UpdatesUpdatedByAndUpdatedAt()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");
        user.Deactivate("admin");
        var before = DateTime.UtcNow;

        // Act
        user.Activate("superadmin");

        // Assert
        user.UpdatedBy.Should().Be("superadmin");
        user.UpdatedAt.Should().NotBeNull();
        user.UpdatedAt!.Value.Should().BeOnOrAfter(before);
    }

    // =====================================================================
    // AddRefreshToken()
    // =====================================================================

    [Fact]
    public void AddRefreshToken_AddsTokenToCollection()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");
        var tokenValue = "refresh-token-value";
        var expiry = DateTime.UtcNow.AddDays(7);

        // Act
        var refreshToken = user.AddRefreshToken(tokenValue, expiry, "127.0.0.1");

        // Assert
        user.RefreshTokens.Should().HaveCount(1);
        refreshToken.Token.Should().Be(tokenValue);
        refreshToken.ExpiresAt.Should().Be(expiry);
        refreshToken.UserId.Should().Be(user.Id);
    }

    [Fact]
    public void AddRefreshToken_MultipleTokens_AddsAllToCollection()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");

        // Act
        user.AddRefreshToken("token-1", DateTime.UtcNow.AddDays(7));
        user.AddRefreshToken("token-2", DateTime.UtcNow.AddDays(7));

        // Assert
        user.RefreshTokens.Should().HaveCount(2);
    }

    // =====================================================================
    // RevokeAllRefreshTokens()
    // =====================================================================

    [Fact]
    public void RevokeAllRefreshTokens_RevokesAllActiveTokens()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");
        user.AddRefreshToken("token-1", DateTime.UtcNow.AddDays(7));
        user.AddRefreshToken("token-2", DateTime.UtcNow.AddDays(7));

        // Act
        user.RevokeAllRefreshTokens();

        // Assert
        user.RefreshTokens.All(t => t.IsRevoked).Should().BeTrue();
    }

    [Fact]
    public void RevokeAllRefreshTokens_WhenNoActiveTokens_DoesNotThrow()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");

        // Act
        var act = () => user.RevokeAllRefreshTokens();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RevokeAllRefreshTokens_AlreadyRevokedTokens_RemainsRevoked()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");
        var token = user.AddRefreshToken("token-1", DateTime.UtcNow.AddDays(7));
        token.Revoke();

        // Act - should not throw even though token is already revoked
        user.RevokeAllRefreshTokens();

        // Assert
        token.IsRevoked.Should().BeTrue();
    }

    // =====================================================================
    // UpdatePhoto()
    // =====================================================================

    [Fact]
    public void UpdatePhoto_WithUrl_SetsPhotoUrl()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");
        var photoUrl = "https://new-photo.url/pic.jpg";

        // Act
        user.UpdatePhoto(photoUrl);

        // Assert
        user.PhotoUrl.Should().Be(photoUrl);
    }

    [Fact]
    public void UpdatePhoto_UpdatesUpdatedAt()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");
        var before = DateTime.UtcNow;

        // Act
        user.UpdatePhoto("https://photo.url");

        // Assert
        user.UpdatedAt.Should().NotBeNull();
        user.UpdatedAt!.Value.Should().BeOnOrAfter(before);
    }

    // =====================================================================
    // BaseEntity properties (Id, CreatedAt, DomainEvents)
    // =====================================================================

    [Fact]
    public void Create_AssignsNonEmptyGuid()
    {
        // Act
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");

        // Assert
        user.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_TwoUsers_HaveDifferentIds()
    {
        // Act
        var user1 = User.Create("user1@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");
        var user2 = User.Create("user2@univ.edu", "Maria", "Lopez", "hash", UserRole.Beca, "admin");

        // Assert
        user1.Id.Should().NotBe(user2.Id);
    }

    [Fact]
    public void RefreshTokens_IsReadOnlyCollection()
    {
        // Arrange
        var user = User.Create("test@univ.edu", "Juan", "Perez", "hash", UserRole.Beca, "admin");

        // Assert
        user.RefreshTokens.Should().BeAssignableTo<IReadOnlyCollection<RefreshToken>>();
    }
}
