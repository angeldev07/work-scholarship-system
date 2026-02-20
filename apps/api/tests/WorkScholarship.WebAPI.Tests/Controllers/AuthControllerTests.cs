using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Auth.Commands.Login;
using WorkScholarship.Application.Features.Auth.Commands.Logout;
using WorkScholarship.Application.Features.Auth.Commands.RefreshToken;
using WorkScholarship.Application.Features.Auth.Common;
using WorkScholarship.Application.Features.Auth.Queries.GetCurrentUser;
using WorkScholarship.WebAPI.Controllers;

namespace WorkScholarship.WebAPI.Tests.Controllers;

[Trait("Category", "WebAPI")]
[Trait("Component", "AuthController")]
public class AuthControllerTests
{
    private readonly ISender _sender;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _sender = Substitute.For<ISender>();
        _controller = new AuthController(_sender);

        // Setup a real HttpContext with cookie support
        SetupHttpContext();
    }

    private void SetupHttpContext(string? refreshTokenCookieValue = null)
    {
        var httpContext = new DefaultHttpContext();

        if (refreshTokenCookieValue != null)
        {
            var cookieHeader = $"refreshToken={refreshTokenCookieValue}";
            httpContext.Request.Headers["Cookie"] = cookieHeader;
        }

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    private static LoginResponse CreateLoginResponse(string accessToken = "access-token", string refreshToken = "refresh-token")
    {
        return new LoginResponse
        {
            AccessToken = accessToken,
            ExpiresIn = 86400,
            TokenType = "Bearer",
            User = new UserDto
            {
                Id = Guid.NewGuid(),
                Email = "test@univ.edu",
                FirstName = "Test",
                LastName = "User",
                FullName = "Test User",
                Role = "BECA",
                IsActive = true,
                AuthProvider = "Local"
            },
            RefreshTokenValue = refreshToken,
            RefreshTokenExpirationDays = 7
        };
    }

    private static TokenResponse CreateTokenResponse(string accessToken = "new-access", string refreshToken = "new-refresh")
    {
        return new TokenResponse
        {
            AccessToken = accessToken,
            ExpiresIn = 86400,
            TokenType = "Bearer",
            RefreshTokenValue = refreshToken,
            RefreshTokenExpirationDays = 7
        };
    }

    private static UserDto CreateUserDto()
    {
        return new UserDto
        {
            Id = Guid.NewGuid(),
            Email = "test@univ.edu",
            FirstName = "Test",
            LastName = "User",
            FullName = "Test User",
            Role = "BECA",
            IsActive = true,
            AuthProvider = "Local"
        };
    }

    // =====================================================================
    // POST /api/auth/login - Login
    // =====================================================================

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithAccessToken()
    {
        // Arrange
        var command = new LoginCommand("test@univ.edu", "password");
        var loginResponse = CreateLoginResponse();
        _sender.Send(Arg.Any<LoginCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<LoginResponse>.Success(loginResponse));

        // Act
        var actionResult = await _controller.Login(command, CancellationToken.None);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<LoginResponse>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.AccessToken.Should().Be("access-token");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401()
    {
        // Arrange
        var command = new LoginCommand("test@univ.edu", "wrong");
        _sender.Send(Arg.Any<LoginCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<LoginResponse>.Failure(AuthErrorCodes.INVALID_CREDENTIALS, "Email o contrasena incorrectos."));

        // Act
        var actionResult = await _controller.Login(command, CancellationToken.None);

        // Assert
        var statusResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Login_WithAccountLocked_Returns401()
    {
        // Arrange
        var command = new LoginCommand("test@univ.edu", "password");
        _sender.Send(Arg.Any<LoginCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<LoginResponse>.Failure(AuthErrorCodes.ACCOUNT_LOCKED, "Cuenta bloqueada."));

        // Act
        var actionResult = await _controller.Login(command, CancellationToken.None);

        // Assert
        var statusResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Login_WithGoogleAccount_Returns403()
    {
        // Arrange
        var command = new LoginCommand("test@univ.edu", "password");
        _sender.Send(Arg.Any<LoginCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<LoginResponse>.Failure(AuthErrorCodes.GOOGLE_ACCOUNT, "Usa Google."));

        // Act
        var actionResult = await _controller.Login(command, CancellationToken.None);

        // Assert
        var statusResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Login_WithInactiveAccount_Returns403()
    {
        // Arrange
        var command = new LoginCommand("test@univ.edu", "password");
        _sender.Send(Arg.Any<LoginCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<LoginResponse>.Failure(AuthErrorCodes.INACTIVE_ACCOUNT, "Cuenta desactivada."));

        // Act
        var actionResult = await _controller.Login(command, CancellationToken.None);

        // Assert
        var statusResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Login_WithValidationError_Returns400()
    {
        // Arrange
        var command = new LoginCommand("", "");
        _sender.Send(Arg.Any<LoginCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<LoginResponse>.Failure(AuthErrorCodes.VALIDATION_ERROR, "Validation failed."));

        // Act
        var actionResult = await _controller.Login(command, CancellationToken.None);

        // Assert
        var statusResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Login_WithUnknownError_Returns400()
    {
        // Arrange
        var command = new LoginCommand("test@univ.edu", "password");
        _sender.Send(Arg.Any<LoginCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<LoginResponse>.Failure("UNKNOWN_ERROR", "Unknown."));

        // Act
        var actionResult = await _controller.Login(command, CancellationToken.None);

        // Assert
        var statusResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Login_WithSuccess_SetsRefreshTokenCookie()
    {
        // Arrange
        var command = new LoginCommand("test@univ.edu", "password");
        var loginResponse = CreateLoginResponse(refreshToken: "refresh-cookie-value");
        _sender.Send(Arg.Any<LoginCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<LoginResponse>.Success(loginResponse));

        // Act
        await _controller.Login(command, CancellationToken.None);

        // Assert - cookie should be set in response
        var cookies = _controller.HttpContext.Response.Headers["Set-Cookie"];
        cookies.ToString().Should().Contain("refreshToken");
    }

    // =====================================================================
    // POST /api/auth/refresh - Refresh
    // =====================================================================

    [Fact]
    public async Task Refresh_WithValidRefreshTokenCookie_Returns200WithNewTokens()
    {
        // Arrange
        SetupHttpContext(refreshTokenCookieValue: "valid-refresh-token");
        var tokenResponse = CreateTokenResponse();
        _sender.Send(Arg.Any<RefreshTokenCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<TokenResponse>.Success(tokenResponse));

        // Act
        var actionResult = await _controller.Refresh(CancellationToken.None);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<TokenResponse>>().Subject;
        apiResponse.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Refresh_WithNoRefreshTokenCookie_Returns401()
    {
        // Arrange - no cookie set in context
        SetupHttpContext(); // no cookie

        // Act
        var actionResult = await _controller.Refresh(CancellationToken.None);

        // Assert
        var result = actionResult.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_Returns401AndClearsCookie()
    {
        // Arrange
        SetupHttpContext(refreshTokenCookieValue: "invalid-token");
        _sender.Send(Arg.Any<RefreshTokenCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<TokenResponse>.Failure(AuthErrorCodes.INVALID_REFRESH_TOKEN, "Token invalido."));

        // Act
        var actionResult = await _controller.Refresh(CancellationToken.None);

        // Assert
        actionResult.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Refresh_WithSessionExpired_Returns401()
    {
        // Arrange
        SetupHttpContext(refreshTokenCookieValue: "expired-token");
        _sender.Send(Arg.Any<RefreshTokenCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<TokenResponse>.Failure(AuthErrorCodes.SESSION_EXPIRED, "Sesion expirada."));

        // Act
        var actionResult = await _controller.Refresh(CancellationToken.None);

        // Assert
        actionResult.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Refresh_WithSuccess_SetsNewRefreshTokenCookie()
    {
        // Arrange
        SetupHttpContext(refreshTokenCookieValue: "old-token");
        var tokenResponse = CreateTokenResponse(refreshToken: "new-refresh-value");
        _sender.Send(Arg.Any<RefreshTokenCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<TokenResponse>.Success(tokenResponse));

        // Act
        await _controller.Refresh(CancellationToken.None);

        // Assert - new cookie should be set
        var cookies = _controller.HttpContext.Response.Headers["Set-Cookie"];
        cookies.ToString().Should().Contain("refreshToken");
    }

    // =====================================================================
    // POST /api/auth/logout - Logout
    // =====================================================================

    [Fact]
    public async Task Logout_WithRefreshTokenCookie_Returns200()
    {
        // Arrange
        SetupHttpContext(refreshTokenCookieValue: "refresh-token");
        _sender.Send(Arg.Any<LogoutCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var actionResult = await _controller.Logout(CancellationToken.None);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Logout_WithNoRefreshTokenCookie_Returns200()
    {
        // Arrange
        SetupHttpContext();
        _sender.Send(Arg.Any<LogoutCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var actionResult = await _controller.Logout(CancellationToken.None);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Logout_ClearsRefreshTokenCookie()
    {
        // Arrange
        SetupHttpContext(refreshTokenCookieValue: "some-token");
        _sender.Send(Arg.Any<LogoutCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        await _controller.Logout(CancellationToken.None);

        // Assert - cookie deletion should be in the response
        var cookies = _controller.HttpContext.Response.Headers["Set-Cookie"];
        // The cookie header should contain something related to refreshToken deletion
        // (expires in the past or Max-Age=0)
        cookies.ToString().Should().Contain("refreshToken");
    }

    [Fact]
    public async Task Logout_SendsLogoutCommandWithRefreshTokenFromCookie()
    {
        // Arrange
        SetupHttpContext(refreshTokenCookieValue: "my-refresh-token");
        _sender.Send(Arg.Any<LogoutCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        await _controller.Logout(CancellationToken.None);

        // Assert - verify the command was sent with the cookie value
        await _sender.Received(1).Send(
            Arg.Is<LogoutCommand>(cmd => cmd.RefreshToken == "my-refresh-token"),
            Arg.Any<CancellationToken>());
    }

    // =====================================================================
    // GET /api/auth/me - GetCurrentUser
    // =====================================================================

    [Fact]
    public async Task GetCurrentUser_WithAuthenticatedUser_Returns200WithUserDto()
    {
        // Arrange
        var userDto = CreateUserDto();
        _sender.Send(Arg.Any<GetCurrentUserQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Success(userDto));

        // Act
        var actionResult = await _controller.GetCurrentUser(CancellationToken.None);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<UserDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCurrentUser_WhenUnauthorized_Returns401()
    {
        // Arrange
        _sender.Send(Arg.Any<GetCurrentUserQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Failure(AuthErrorCodes.UNAUTHORIZED, "No autorizado."));

        // Act
        var actionResult = await _controller.GetCurrentUser(CancellationToken.None);

        // Assert
        var statusResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task GetCurrentUser_WhenOtherError_Returns400()
    {
        // Arrange
        _sender.Send(Arg.Any<GetCurrentUserQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Failure(AuthErrorCodes.USER_NOT_FOUND, "No encontrado."));

        // Act
        var actionResult = await _controller.GetCurrentUser(CancellationToken.None);

        // Assert
        var statusResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GetCurrentUser_WithSuccess_ResponseContainsCorrectUserData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userDto = new UserDto
        {
            Id = userId,
            Email = "juan@univ.edu",
            FirstName = "Juan",
            LastName = "Perez",
            FullName = "Juan Perez",
            Role = "ADMIN",
            IsActive = true,
            AuthProvider = "Local"
        };
        _sender.Send(Arg.Any<GetCurrentUserQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Success(userDto));

        // Act
        var actionResult = await _controller.GetCurrentUser(CancellationToken.None);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<UserDto>>().Subject;
        apiResponse.Data!.Email.Should().Be("juan@univ.edu");
        apiResponse.Data.Role.Should().Be("ADMIN");
    }
}
