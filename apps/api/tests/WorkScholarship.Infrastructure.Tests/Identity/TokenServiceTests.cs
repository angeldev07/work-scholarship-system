using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using WorkScholarship.Domain.Entities;
using WorkScholarship.Domain.Enums;
using WorkScholarship.Infrastructure.Identity;

namespace WorkScholarship.Infrastructure.Tests.Identity;

[Trait("Category", "Infrastructure")]
[Trait("Component", "TokenService")]
public class TokenServiceTests
{
    private readonly TokenService _tokenService;
    private readonly IConfiguration _configuration;

    public TokenServiceTests()
    {
        var settings = new Dictionary<string, string?>
        {
            { "Jwt:Secret", "this-is-a-very-long-secret-key-for-testing-that-is-at-least-32-chars" },
            { "Jwt:Issuer", "WorkScholarship.Test" },
            { "Jwt:Audience", "WorkScholarship.TestClient" },
            { "Jwt:AccessTokenExpirationInSeconds", "3600" },
            { "Jwt:RefreshTokenExpirationInDays", "14" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        _tokenService = new TokenService(_configuration);
    }

    // =====================================================================
    // GenerateAccessToken()
    // =====================================================================

    [Fact]
    public void GenerateAccessToken_ReturnsNonEmptyString()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var token = _tokenService.GenerateAccessToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateAccessToken_ReturnsValidJwtStructure()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var token = _tokenService.GenerateAccessToken(user);

        // Assert - JWT has 3 parts separated by dots
        var parts = token.Split('.');
        parts.Should().HaveCount(3);
    }

    [Fact]
    public void GenerateAccessToken_ContainsSubClaim()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var token = _tokenService.GenerateAccessToken(user);
        var claims = ParseTokenClaims(token);

        // Assert
        var subClaim = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        subClaim.Should().NotBeNull();
        subClaim!.Value.Should().Be(user.Id.ToString());
    }

    [Fact]
    public void GenerateAccessToken_ContainsEmailClaim()
    {
        // Arrange
        var user = CreateTestUser(email: "juan.perez@univ.edu");

        // Act
        var token = _tokenService.GenerateAccessToken(user);
        var claims = ParseTokenClaims(token);

        // Assert
        var emailClaim = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        emailClaim.Should().NotBeNull();
        emailClaim!.Value.Should().Be("juan.perez@univ.edu");
    }

    [Fact]
    public void GenerateAccessToken_ContainsJtiClaim()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var token = _tokenService.GenerateAccessToken(user);
        var claims = ParseTokenClaims(token);

        // Assert
        var jtiClaim = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
        jtiClaim.Should().NotBeNull();
        Guid.TryParse(jtiClaim!.Value, out _).Should().BeTrue();
    }

    [Fact]
    public void GenerateAccessToken_ContainsRoleClaim()
    {
        // Arrange
        var user = CreateTestUser(role: UserRole.Admin);

        // Act
        var token = _tokenService.GenerateAccessToken(user);
        var claims = ParseTokenClaims(token);

        // Assert
        var roleClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        roleClaim.Should().NotBeNull();
        roleClaim!.Value.Should().Be($"{UserRole.Admin}");
    }

    [Theory]
    [InlineData(UserRole.Admin, "Admin")]
    [InlineData(UserRole.Supervisor, "Supervisor")]
    [InlineData(UserRole.Beca, "Beca")]
    [InlineData(UserRole.None, "None")]
    public void GenerateAccessToken_MapsRoleToCorrectString(UserRole role, string expectedRoleName)
    {
        // Arrange
        var user = CreateTestUser(role: role);

        // Act
        var token = _tokenService.GenerateAccessToken(user);
        var claims = ParseTokenClaims(token);

        // Assert
        var roleClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        roleClaim!.Value.Should().Be(expectedRoleName);
    }

    [Fact]
    public void GenerateAccessToken_ContainsFirstNameClaim()
    {
        // Arrange
        var user = CreateTestUser(firstName: "Juan");

        // Act
        var token = _tokenService.GenerateAccessToken(user);
        var claims = ParseTokenClaims(token);

        // Assert
        var firstNameClaim = claims.FirstOrDefault(c => c.Type == "firstName");
        firstNameClaim.Should().NotBeNull();
        firstNameClaim!.Value.Should().Be("Juan");
    }

    [Fact]
    public void GenerateAccessToken_ContainsLastNameClaim()
    {
        // Arrange
        var user = CreateTestUser(lastName: "Perez");

        // Act
        var token = _tokenService.GenerateAccessToken(user);
        var claims = ParseTokenClaims(token);

        // Assert
        var lastNameClaim = claims.FirstOrDefault(c => c.Type == "lastName");
        lastNameClaim.Should().NotBeNull();
        lastNameClaim!.Value.Should().Be("Perez");
    }

    [Fact]
    public void GenerateAccessToken_TwoDifferentUsers_GenerateDifferentTokens()
    {
        // Arrange
        var user1 = CreateTestUser(email: "user1@univ.edu");
        var user2 = CreateTestUser(email: "user2@univ.edu");

        // Act
        var token1 = _tokenService.GenerateAccessToken(user1);
        var token2 = _tokenService.GenerateAccessToken(user2);

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void GenerateAccessToken_SameUserCalledTwice_GeneratesDifferentTokens()
    {
        // Arrange - different JTI each call
        var user = CreateTestUser();

        // Act
        var token1 = _tokenService.GenerateAccessToken(user);
        var token2 = _tokenService.GenerateAccessToken(user);

        // Assert - different JTI (Guid.NewGuid() each time)
        token1.Should().NotBe(token2);
    }

    // =====================================================================
    // GenerateRefreshToken()
    // =====================================================================

    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyString()
    {
        // Act
        var token = _tokenService.GenerateRefreshToken();

        // Assert
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsBase64String()
    {
        // Act
        var token = _tokenService.GenerateRefreshToken();

        // Assert - should be valid base64
        var act = () => Convert.FromBase64String(token);
        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateRefreshToken_Returns64ByteBase64()
    {
        // Act
        var token = _tokenService.GenerateRefreshToken();
        var bytes = Convert.FromBase64String(token);

        // Assert - 64 random bytes
        bytes.Length.Should().Be(64);
    }

    [Fact]
    public void GenerateRefreshToken_CalledMultipleTimes_ReturnsDifferentValues()
    {
        // Act
        var tokens = Enumerable.Range(0, 10)
            .Select(_ => _tokenService.GenerateRefreshToken())
            .ToList();

        // Assert - all tokens should be unique (cryptographically random)
        tokens.Distinct().Should().HaveCount(10);
    }

    // =====================================================================
    // GetAccessTokenExpirationInSeconds()
    // =====================================================================

    [Fact]
    public void GetAccessTokenExpirationInSeconds_ReturnsConfiguredValue()
    {
        // Act
        var result = _tokenService.GetAccessTokenExpirationInSeconds();

        // Assert
        result.Should().Be(3600);
    }

    [Fact]
    public void GetAccessTokenExpirationInSeconds_WhenNotConfigured_ReturnsDefault()
    {
        // Arrange - configuration without the setting
        var emptyConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:Secret", "this-is-a-very-long-secret-key-for-testing-that-is-at-least-32-chars" },
                { "Jwt:Issuer", "Test" },
                { "Jwt:Audience", "Test" }
                // No AccessTokenExpirationInSeconds
            })
            .Build();
        var service = new TokenService(emptyConfig);

        // Act
        var result = service.GetAccessTokenExpirationInSeconds();

        // Assert - default is 86400 (24h)
        result.Should().Be(86400);
    }

    // =====================================================================
    // GetRefreshTokenExpirationInDays()
    // =====================================================================

    [Fact]
    public void GetRefreshTokenExpirationInDays_ReturnsConfiguredValue()
    {
        // Act
        var result = _tokenService.GetRefreshTokenExpirationInDays();

        // Assert
        result.Should().Be(14);
    }

    [Fact]
    public void GetRefreshTokenExpirationInDays_WhenNotConfigured_ReturnsDefault()
    {
        // Arrange - configuration without the setting
        var emptyConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:Secret", "this-is-a-very-long-secret-key-for-testing-that-is-at-least-32-chars" },
                { "Jwt:Issuer", "Test" },
                { "Jwt:Audience", "Test" }
                // No RefreshTokenExpirationInDays
            })
            .Build();
        var service = new TokenService(emptyConfig);

        // Act
        var result = service.GetRefreshTokenExpirationInDays();

        // Assert - default is 7
        result.Should().Be(7);
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    private static User CreateTestUser(
        string email = "test@univ.edu",
        string firstName = "Test",
        string lastName = "User",
        UserRole role = UserRole.Beca)
    {
        return User.Create(email, firstName, lastName, "hashed_password", role, "system");
    }

    private static IEnumerable<Claim> ParseTokenClaims(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        return jwt.Claims;
    }
}
