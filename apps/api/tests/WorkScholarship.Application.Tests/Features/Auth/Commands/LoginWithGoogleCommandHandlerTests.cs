using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Auth.Commands.LoginWithGoogle;
using WorkScholarship.Application.Tests.TestHelpers;
using WorkScholarship.Domain.Enums;
using WorkScholarship.Infrastructure.Data;

namespace WorkScholarship.Application.Tests.Features.Auth.Commands;

[Trait("Category", "Application")]
[Trait("Feature", "Auth")]
[Trait("Component", "LoginWithGoogleCommandHandler")]
public class LoginWithGoogleCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly ITokenService _tokenService;
    private readonly LoginWithGoogleCommandHandler _handler;

    private const string DEFAULT_CODE = "google-auth-code-123";
    private const string DEFAULT_REDIRECT_URI = "https://localhost:7001/api/auth/google/callback";

    public LoginWithGoogleCommandHandlerTests()
    {
        _dbContext = DbContextFactory.CreateInMemoryContext();
        _googleAuthService = Substitute.For<IGoogleAuthService>();
        _tokenService = Substitute.For<ITokenService>();

        // Default token service setup
        _tokenService.GenerateAccessToken(Arg.Any<Domain.Entities.User>()).Returns("jwt-access-token");
        _tokenService.GenerateRefreshToken().Returns("refresh-token-value");
        _tokenService.GetRefreshTokenExpirationInDays().Returns(7);
        _tokenService.GetAccessTokenExpirationInSeconds().Returns(86400);

        var googleSettings = Options.Create(new GoogleAuthSettings
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            AllowedDomains = []
        });

        _handler = new LoginWithGoogleCommandHandler(
            _dbContext,
            _googleAuthService,
            _tokenService,
            googleSettings);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    /// <summary>
    /// Crea un handler con configuración de dominios permitidos para tests de validación de dominio.
    /// </summary>
    private LoginWithGoogleCommandHandler CreateHandlerWithDomainRestriction(params string[] allowedDomains)
    {
        var settings = Options.Create(new GoogleAuthSettings
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            AllowedDomains = [..allowedDomains]
        });

        return new LoginWithGoogleCommandHandler(
            _dbContext,
            _googleAuthService,
            _tokenService,
            settings);
    }

    private static GoogleUserInfo CreateGoogleUserInfo(
        string email = "juan.perez@univ.edu",
        string firstName = "Juan",
        string lastName = "Perez",
        string googleId = "google-sub-123",
        string? photoUrl = "https://lh3.googleusercontent.com/photo.jpg")
    {
        return new GoogleUserInfo(email, firstName, lastName, googleId, photoUrl);
    }

    // =====================================================================
    // Happy path - Nuevo usuario Google
    // =====================================================================

    [Fact]
    public async Task Handle_WithNewGoogleUser_CreatesUserAndReturnsSuccess()
    {
        // Arrange
        var googleUser = CreateGoogleUserInfo();
        _googleAuthService
            .ExchangeCodeForUserInfoAsync(DEFAULT_CODE, DEFAULT_REDIRECT_URI, Arg.Any<CancellationToken>())
            .Returns(googleUser);

        var command = new LoginWithGoogleCommand(DEFAULT_CODE, DEFAULT_REDIRECT_URI);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("jwt-access-token");
        result.Value.TokenType.Should().Be("Bearer");
        result.Value.ExpiresIn.Should().Be(86400);
    }

    [Fact]
    public async Task Handle_WithNewGoogleUser_CreatesUserInDatabase()
    {
        // Arrange
        var googleUser = CreateGoogleUserInfo(
            email: "nuevo@univ.edu",
            firstName: "Nuevo",
            lastName: "Usuario",
            googleId: "google-new-123",
            photoUrl: "https://photo.url/new.jpg");
        _googleAuthService
            .ExchangeCodeForUserInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(googleUser);

        var command = new LoginWithGoogleCommand(DEFAULT_CODE, DEFAULT_REDIRECT_URI);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var createdUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == "nuevo@univ.edu");
        createdUser.Should().NotBeNull();
        createdUser!.FirstName.Should().Be("Nuevo");
        createdUser.LastName.Should().Be("Usuario");
        createdUser.GoogleId.Should().Be("google-new-123");
        createdUser.PhotoUrl.Should().Be("https://photo.url/new.jpg");
        createdUser.AuthProvider.Should().Be(AuthProvider.Google);
    }

    [Fact]
    public async Task Handle_WithNewGoogleUser_SetsRoleToNone()
    {
        // Arrange
        var googleUser = CreateGoogleUserInfo(email: "new.user@univ.edu");
        _googleAuthService
            .ExchangeCodeForUserInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(googleUser);

        var command = new LoginWithGoogleCommand(DEFAULT_CODE, DEFAULT_REDIRECT_URI);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == "new.user@univ.edu");
        user!.Role.Should().Be(UserRole.None);
    }

    [Fact]
    public async Task Handle_WithNewGoogleUser_ReturnsUserDtoInResponse()
    {
        // Arrange
        var googleUser = CreateGoogleUserInfo(
            email: "dto@univ.edu",
            firstName: "Dto",
            lastName: "Test");
        _googleAuthService
            .ExchangeCodeForUserInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(googleUser);

        var command = new LoginWithGoogleCommand(DEFAULT_CODE, DEFAULT_REDIRECT_URI);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value.User.Should().NotBeNull();
        result.Value.User.Email.Should().Be("dto@univ.edu");
        result.Value.User.FirstName.Should().Be("Dto");
        result.Value.User.LastName.Should().Be("Test");
        result.Value.User.AuthProvider.Should().Be("Google");
    }

    [Fact]
    public async Task Handle_WithNewGoogleUser_AddsRefreshToken()
    {
        // Arrange
        var googleUser = CreateGoogleUserInfo(email: "refresh@univ.edu");
        _googleAuthService
            .ExchangeCodeForUserInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(googleUser);

        var command = new LoginWithGoogleCommand(DEFAULT_CODE, DEFAULT_REDIRECT_URI);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var tokens = await _dbContext.RefreshTokens.ToListAsync();
        tokens.Should().HaveCount(1);
        tokens[0].Token.Should().Be("refresh-token-value");
    }

    [Fact]
    public async Task Handle_WithNewGoogleUser_RecordsSuccessfulLogin()
    {
        // Arrange
        var googleUser = CreateGoogleUserInfo(email: "login@univ.edu");
        _googleAuthService
            .ExchangeCodeForUserInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(googleUser);

        var command = new LoginWithGoogleCommand(DEFAULT_CODE, DEFAULT_REDIRECT_URI);
        var beforeLogin = DateTime.UtcNow;

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == "login@univ.edu");
        user!.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt!.Value.Should().BeOnOrAfter(beforeLogin);
    }

    [Fact]
    public async Task Handle_WithNewGoogleUser_ReturnsRefreshTokenValueInResponse()
    {
        // Arrange
        var googleUser = CreateGoogleUserInfo();
        _googleAuthService
            .ExchangeCodeForUserInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(googleUser);

        var command = new LoginWithGoogleCommand(DEFAULT_CODE, DEFAULT_REDIRECT_URI);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value.RefreshTokenValue.Should().Be("refresh-token-value");
        result.Value.RefreshTokenExpirationDays.Should().Be(7);
    }

    // =====================================================================
    // Existing user with Local AuthProvider - Link Google account
    // =====================================================================

    [Fact]
    public async Task Handle_WithExistingLocalUser_LinksGoogleAccount()
    {
        // Arrange
        var existingUser = new UserBuilder()
            .WithEmail("local@univ.edu")
            .WithFirstName("Local")
            .WithLastName("User")
            .WithPasswordHash("hashed_password")
            .WithRole(UserRole.Beca)
            .Build();
        _dbContext.Users.Add(existingUser);
        await _dbContext.SaveChangesAsync();

        var googleUser = CreateGoogleUserInfo(
            email: "local@univ.edu",
            googleId: "google-link-456",
            photoUrl: "https://photo.url/linked.jpg");
        _googleAuthService
            .ExchangeCodeForUserInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(googleUser);

        var command = new LoginWithGoogleCommand(DEFAULT_CODE, DEFAULT_REDIRECT_URI);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == "local@univ.edu");
        updatedUser!.GoogleId.Should().Be("google-link-456");
        updatedUser.AuthProvider.Should().Be(AuthProvider.Google);
        updatedUser.PhotoUrl.Should().Be("https://photo.url/linked.jpg");
    }

    [Fact]
    public async Task Handle_WithExistingLocalUser_PreservesExistingRole()
    {
        // Arrange
        var existingUser = new UserBuilder()
            .WithEmail("role.test@univ.edu")
            .WithRole(UserRole.Admin)
            .WithPasswordHash("hash")
            .Build();
        _dbContext.Users.Add(existingUser);
        await _dbContext.SaveChangesAsync();

        var googleUser = CreateGoogleUserInfo(email: "role.test@univ.edu");
        _googleAuthService
            .ExchangeCodeForUserInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(googleUser);

        var command = new LoginWithGoogleCommand(DEFAULT_CODE, DEFAULT_REDIRECT_URI);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value.User.Role.Should().Be(UserRole.Admin);
    }

    // =====================================================================
    // Existing user with Google AuthProvider - Direct login
    // =====================================================================

    [Fact]
    public async Task Handle_WithExistingGoogleUser_LoginsDirect()
    {
        // Arrange
        var existingUser = new UserBuilder()
            .WithEmail("google.user@univ.edu")
            .AsGoogle(googleId: "google-existing-789")
            .Build();
        // Assign a real role to the Google user
        existingUser.ChangeRole(UserRole.Beca, "system");
        _dbContext.Users.Add(existingUser);
        await _dbContext.SaveChangesAsync();

        var googleUser = CreateGoogleUserInfo(
            email: "google.user@univ.edu",
            googleId: "google-existing-789");
        _googleAuthService
            .ExchangeCodeForUserInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(googleUser);

        var command = new LoginWithGoogleCommand(DEFAULT_CODE, DEFAULT_REDIRECT_URI);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.User.Email.Should().Be("google.user@univ.edu");
    }

    [Fact]
    public async Task Handle_WithExistingGoogleUser_DoesNotCreateDuplicateUser()
    {
        // Arrange
        var existingUser = new UserBuilder()
            .WithEmail("unique@univ.edu")
            .AsGoogle()
            .Build();
        _dbContext.Users.Add(existingUser);
        await _dbContext.SaveChangesAsync();

        var googleUser = CreateGoogleUserInfo(email: "unique@univ.edu");
        _googleAuthService
            .ExchangeCodeForUserInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(googleUser);

        var command = new LoginWithGoogleCommand(DEFAULT_CODE, DEFAULT_REDIRECT_URI);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var userCount = await _dbContext.Users.CountAsync(u => u.Email == "unique@univ.edu");
        userCount.Should().Be(1);
    }

    // =====================================================================
    // Error paths - OAuth failure
    // =====================================================================

    [Fact]
    public async Task Handle_WhenGoogleExchangeFails_ReturnsOAuthFailedError()
    {
        // Arrange
        _googleAuthService
            .ExchangeCodeForUserInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((GoogleUserInfo?)null);

        var command = new LoginWithGoogleCommand(DEFAULT_CODE, DEFAULT_REDIRECT_URI);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AuthErrorCodes.OAUTH_FAILED);
    }

    [Fact]
    public async Task Handle_WhenGoogleExchangeFails_DoesNotCreateUser()
    {
        // Arrange
        _googleAuthService
            .ExchangeCodeForUserInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((GoogleUserInfo?)null);

        var command = new LoginWithGoogleCommand(DEFAULT_CODE, DEFAULT_REDIRECT_URI);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var userCount = await _dbContext.Users.CountAsync();
        userCount.Should().Be(0);
    }

    // =====================================================================
    // Error paths - Invalid domain
    // =====================================================================

    [Fact]
    public async Task Handle_WithInvalidDomain_ReturnsInvalidDomainError()
    {
        // Arrange
        var handler = CreateHandlerWithDomainRestriction("universidad.edu");

        var googleUser = CreateGoogleUserInfo(email: "user@gmail.com");
        _googleAuthService
            .ExchangeCodeForUserInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(googleUser);

        var command = new LoginWithGoogleCommand(DEFAULT_CODE, DEFAULT_REDIRECT_URI);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AuthErrorCodes.INVALID_DOMAIN);
        result.Error.Message.Should().Contain("universidad.edu");
    }

    [Fact]
    public async Task Handle_WithValidDomain_ReturnsSuccess()
    {
        // Arrange
        var handler = CreateHandlerWithDomainRestriction("universidad.edu");

        var googleUser = CreateGoogleUserInfo(email: "student@universidad.edu");
        _googleAuthService
            .ExchangeCodeForUserInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(googleUser);

        var command = new LoginWithGoogleCommand(DEFAULT_CODE, DEFAULT_REDIRECT_URI);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithEmptyAllowedDomains_AllowsAnyDomain()
    {
        // Arrange - default handler has empty AllowedDomains
        var googleUser = CreateGoogleUserInfo(email: "anyone@gmail.com");
        _googleAuthService
            .ExchangeCodeForUserInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(googleUser);

        var command = new LoginWithGoogleCommand(DEFAULT_CODE, DEFAULT_REDIRECT_URI);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithDomainCheckCaseInsensitive_ReturnsSuccess()
    {
        // Arrange
        var handler = CreateHandlerWithDomainRestriction("Universidad.Edu");

        var googleUser = CreateGoogleUserInfo(email: "user@UNIVERSIDAD.EDU");
        _googleAuthService
            .ExchangeCodeForUserInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(googleUser);

        var command = new LoginWithGoogleCommand(DEFAULT_CODE, DEFAULT_REDIRECT_URI);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    // =====================================================================
    // Error paths - Inactive account
    // =====================================================================

    [Fact]
    public async Task Handle_WithInactiveExistingUser_ReturnsInactiveAccountError()
    {
        // Arrange
        var inactiveUser = new UserBuilder()
            .WithEmail("inactive@univ.edu")
            .AsGoogle()
            .AsInactive()
            .Build();
        _dbContext.Users.Add(inactiveUser);
        await _dbContext.SaveChangesAsync();

        var googleUser = CreateGoogleUserInfo(email: "inactive@univ.edu");
        _googleAuthService
            .ExchangeCodeForUserInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(googleUser);

        var command = new LoginWithGoogleCommand(DEFAULT_CODE, DEFAULT_REDIRECT_URI);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AuthErrorCodes.INACTIVE_ACCOUNT);
    }

    [Fact]
    public async Task Handle_WithInactiveLocalUserLinked_ReturnsInactiveAccountError()
    {
        // Arrange
        var inactiveLocalUser = new UserBuilder()
            .WithEmail("inactive.local@univ.edu")
            .WithPasswordHash("hash")
            .AsInactive()
            .Build();
        _dbContext.Users.Add(inactiveLocalUser);
        await _dbContext.SaveChangesAsync();

        var googleUser = CreateGoogleUserInfo(email: "inactive.local@univ.edu");
        _googleAuthService
            .ExchangeCodeForUserInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(googleUser);

        var command = new LoginWithGoogleCommand(DEFAULT_CODE, DEFAULT_REDIRECT_URI);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AuthErrorCodes.INACTIVE_ACCOUNT);
    }

    // =====================================================================
    // Email normalization
    // =====================================================================

    [Fact]
    public async Task Handle_NormalizesEmailToLowercase()
    {
        // Arrange
        var googleUser = CreateGoogleUserInfo(email: "Juan.PEREZ@Univ.Edu");
        _googleAuthService
            .ExchangeCodeForUserInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(googleUser);

        var command = new LoginWithGoogleCommand(DEFAULT_CODE, DEFAULT_REDIRECT_URI);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var user = await _dbContext.Users.FirstAsync();
        user.Email.Should().Be("juan.perez@univ.edu");
    }

    [Fact]
    public async Task Handle_MatchesExistingUserByNormalizedEmail()
    {
        // Arrange
        var existingUser = new UserBuilder()
            .WithEmail("existing@univ.edu")
            .AsGoogle()
            .Build();
        _dbContext.Users.Add(existingUser);
        await _dbContext.SaveChangesAsync();

        var googleUser = CreateGoogleUserInfo(email: "EXISTING@UNIV.EDU");
        _googleAuthService
            .ExchangeCodeForUserInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(googleUser);

        var command = new LoginWithGoogleCommand(DEFAULT_CODE, DEFAULT_REDIRECT_URI);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var userCount = await _dbContext.Users.CountAsync();
        userCount.Should().Be(1); // Should not create a duplicate
    }

    // =====================================================================
    // Token generation verification
    // =====================================================================

    [Fact]
    public async Task Handle_GeneratesAccessTokenForUser()
    {
        // Arrange
        var googleUser = CreateGoogleUserInfo();
        _googleAuthService
            .ExchangeCodeForUserInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(googleUser);

        var command = new LoginWithGoogleCommand(DEFAULT_CODE, DEFAULT_REDIRECT_URI);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _tokenService.Received(1).GenerateAccessToken(Arg.Any<Domain.Entities.User>());
        _tokenService.Received(1).GenerateRefreshToken();
    }

    [Fact]
    public async Task Handle_PassesCorrectCodeToGoogleService()
    {
        // Arrange
        var googleUser = CreateGoogleUserInfo();
        _googleAuthService
            .ExchangeCodeForUserInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(googleUser);

        var command = new LoginWithGoogleCommand("specific-auth-code", "https://my-redirect-uri/callback");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _googleAuthService.Received(1).ExchangeCodeForUserInfoAsync(
            "specific-auth-code",
            "https://my-redirect-uri/callback",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SavesChangesToDatabase()
    {
        // Arrange
        var googleUser = CreateGoogleUserInfo(email: "save.test@univ.edu");
        _googleAuthService
            .ExchangeCodeForUserInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(googleUser);

        var command = new LoginWithGoogleCommand(DEFAULT_CODE, DEFAULT_REDIRECT_URI);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var users = await _dbContext.Users.CountAsync();
        users.Should().Be(1);
        var refreshTokens = await _dbContext.RefreshTokens.CountAsync();
        refreshTokens.Should().Be(1);
    }
}
