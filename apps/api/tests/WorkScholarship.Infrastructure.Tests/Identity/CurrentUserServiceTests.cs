using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using WorkScholarship.Infrastructure.Identity;

namespace WorkScholarship.Infrastructure.Tests.Identity;

[Trait("Category", "Infrastructure")]
[Trait("Component", "CurrentUserService")]
public class CurrentUserServiceTests
{
    // =====================================================================
    // Helpers - Use DefaultHttpContext to avoid NSubstitute issues with
    // non-virtual HttpContext.User property
    // =====================================================================

    private static IHttpContextAccessor CreateAccessorWithUser(ClaimsPrincipal user, bool isAuthenticated = true)
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        var context = new DefaultHttpContext();

        // Assign the real ClaimsPrincipal with authenticated identity
        context.User = user;

        accessor.HttpContext.Returns(context);
        return accessor;
    }

    private static IHttpContextAccessor CreateAccessorWithAuthenticatedIdentity(bool isAuthenticated)
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        var context = new DefaultHttpContext();

        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims, isAuthenticated ? "TestAuth" : null);
        context.User = new ClaimsPrincipal(identity);

        accessor.HttpContext.Returns(context);
        return accessor;
    }

    private static ClaimsPrincipal CreatePrincipal(
        Guid? userId = null,
        string? email = null,
        string? role = null)
    {
        var claims = new List<Claim>();

        if (userId.HasValue)
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, userId.Value.ToString()));

        if (email != null)
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, email));

        if (role != null)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    // =====================================================================
    // UserId
    // =====================================================================

    [Fact]
    public void UserId_WithValidSubClaim_ReturnsGuid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var principal = CreatePrincipal(userId: userId);
        var accessor = CreateAccessorWithUser(principal);
        var service = new CurrentUserService(accessor);

        // Act
        var result = service.UserId;

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(userId);
    }

    [Fact]
    public void UserId_WithNoHttpContext_ReturnsNull()
    {
        // Arrange
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);
        var service = new CurrentUserService(accessor);

        // Act
        var result = service.UserId;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void UserId_WithNoClaims_ReturnsNull()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var accessor = CreateAccessorWithUser(principal);
        var service = new CurrentUserService(accessor);

        // Act
        var result = service.UserId;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void UserId_WithInvalidGuidInSubClaim_ReturnsNull()
    {
        // Arrange
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, "not-a-valid-guid") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var accessor = CreateAccessorWithUser(principal);
        var service = new CurrentUserService(accessor);

        // Act
        var result = service.UserId;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void UserId_WithNameIdentifierFallback_ReturnsGuid()
    {
        // Arrange - use ClaimTypes.NameIdentifier instead of Sub
        var userId = Guid.NewGuid();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var accessor = CreateAccessorWithUser(principal);
        var service = new CurrentUserService(accessor);

        // Act
        var result = service.UserId;

        // Assert
        result.Should().Be(userId);
    }

    // =====================================================================
    // Email
    // =====================================================================

    [Fact]
    public void Email_WithEmailClaim_ReturnsEmail()
    {
        // Arrange
        var principal = CreatePrincipal(email: "test@univ.edu");
        var accessor = CreateAccessorWithUser(principal);
        var service = new CurrentUserService(accessor);

        // Act
        var result = service.Email;

        // Assert
        result.Should().Be("test@univ.edu");
    }

    [Fact]
    public void Email_WithNoClaims_ReturnsNull()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var accessor = CreateAccessorWithUser(principal);
        var service = new CurrentUserService(accessor);

        // Act
        var result = service.Email;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Email_WithNoHttpContext_ReturnsNull()
    {
        // Arrange
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);
        var service = new CurrentUserService(accessor);

        // Act
        var result = service.Email;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Email_WithClaimsTypeEmailFallback_ReturnsEmail()
    {
        // Arrange - use ClaimTypes.Email instead of JwtRegisteredClaimNames.Email
        var claims = new[] { new Claim(ClaimTypes.Email, "fallback@univ.edu") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var accessor = CreateAccessorWithUser(principal);
        var service = new CurrentUserService(accessor);

        // Act
        var result = service.Email;

        // Assert
        result.Should().Be("fallback@univ.edu");
    }

    // =====================================================================
    // Role
    // =====================================================================

    [Fact]
    public void Role_WithRoleClaim_ReturnsRole()
    {
        // Arrange
        var principal = CreatePrincipal(role: "ADMIN");
        var accessor = CreateAccessorWithUser(principal);
        var service = new CurrentUserService(accessor);

        // Act
        var result = service.Role;

        // Assert
        result.Should().Be("ADMIN");
    }

    [Fact]
    public void Role_WithNoClaims_ReturnsNull()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var accessor = CreateAccessorWithUser(principal);
        var service = new CurrentUserService(accessor);

        // Act
        var result = service.Role;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Role_WithNoHttpContext_ReturnsNull()
    {
        // Arrange
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);
        var service = new CurrentUserService(accessor);

        // Act
        var result = service.Role;

        // Assert
        result.Should().BeNull();
    }

    // =====================================================================
    // IsAuthenticated
    // =====================================================================

    [Fact]
    public void IsAuthenticated_WithAuthenticatedUser_ReturnsTrue()
    {
        // Arrange - authenticated identity has an authenticationType set
        var accessor = CreateAccessorWithAuthenticatedIdentity(true);
        var service = new CurrentUserService(accessor);

        // Act
        var result = service.IsAuthenticated;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_WithUnauthenticatedUser_ReturnsFalse()
    {
        // Arrange - no authenticationType = not authenticated
        var accessor = CreateAccessorWithAuthenticatedIdentity(false);
        var service = new CurrentUserService(accessor);

        // Act
        var result = service.IsAuthenticated;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_WithNoHttpContext_ReturnsFalse()
    {
        // Arrange
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);
        var service = new CurrentUserService(accessor);

        // Act
        var result = service.IsAuthenticated;

        // Assert
        result.Should().BeFalse();
    }

    // =====================================================================
    // All claims together
    // =====================================================================

    [Fact]
    public void AllProperties_WithFullyAuthenticatedUser_ReturnCorrectValues()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var principal = CreatePrincipal(userId, "admin@univ.edu", "ADMIN");
        var accessor = CreateAccessorWithUser(principal, isAuthenticated: true);
        var service = new CurrentUserService(accessor);

        // Assert
        service.UserId.Should().Be(userId);
        service.Email.Should().Be("admin@univ.edu");
        service.Role.Should().Be("ADMIN");
        // IsAuthenticated is checked via identity.IsAuthenticated which depends on authenticationType
        service.IsAuthenticated.Should().BeTrue();
    }
}
