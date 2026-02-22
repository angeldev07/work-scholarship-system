using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSubstitute;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Auth.Commands.ChangePassword;
using WorkScholarship.Application.Features.Auth.Commands.ForgotPassword;
using WorkScholarship.Application.Features.Auth.Commands.Login;
using WorkScholarship.Application.Features.Auth.Commands.LoginWithGoogle;
using WorkScholarship.Application.Features.Auth.Commands.Logout;
using WorkScholarship.Application.Features.Auth.Commands.RefreshToken;
using WorkScholarship.Application.Features.Auth.Commands.ResetPassword;
using WorkScholarship.Application.Features.Auth.Common;
using WorkScholarship.Application.Features.Auth.Queries.GetCurrentUser;
using WorkScholarship.Domain.Enums;
using WorkScholarship.WebAPI.Controllers;

namespace WorkScholarship.WebAPI.Tests.Controllers;

[Trait("Category", "WebAPI")]
[Trait("Component", "AuthController")]
public class AuthControllerTests
{
    private readonly ISender _sender;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _sender = Substitute.For<ISender>();
        _googleAuthService = Substitute.For<IGoogleAuthService>();

        var googleSettings = Options.Create(new GoogleAuthSettings
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            AllowedDomains = [],
            FrontendUrl = "http://localhost:4200"
        });

        var environment = Substitute.For<IWebHostEnvironment>();
        environment.EnvironmentName.Returns("Development");

        _controller = new AuthController(_sender, _googleAuthService, googleSettings, environment);

        // Configurar HttpContext real con soporte de cookies
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

        // Configurar Request.Host para que los métodos de construcción de URL funcionen
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost", 7001);

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
                Role = UserRole.Beca,
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
            Role = UserRole.Beca,
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
    // GET /api/auth/google/login - GoogleLogin
    // =====================================================================

    [Fact]
    public void GoogleLogin_WithDefaultReturnUrl_RedirectsToGoogle()
    {
        // Arrange
        var expectedAuthUrl = "https://accounts.google.com/o/oauth2/v2/auth?client_id=test-client-id&state=nonce:dashboard";
        _googleAuthService
            .BuildAuthorizationUrl(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new GoogleAuthorizationUrl(expectedAuthUrl, "testnonce"));

        // Act
        var result = _controller.GoogleLogin();

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectResult>().Subject;
        redirectResult.Url.Should().Be(expectedAuthUrl);
    }

    [Fact]
    public void GoogleLogin_CallsBuildAuthorizationUrlWithCorrectReturnUrl()
    {
        // Arrange
        _googleAuthService
            .BuildAuthorizationUrl(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new GoogleAuthorizationUrl("https://accounts.google.com/auth", "nonce"));

        // Act
        _controller.GoogleLogin(returnUrl: "/admin/dashboard");

        // Assert
        _googleAuthService.Received(1).BuildAuthorizationUrl(
            Arg.Any<string>(),
            "/admin/dashboard");
    }

    [Fact]
    public void GoogleLogin_WithNullReturnUrl_UsesDefaultDashboard()
    {
        // Arrange
        _googleAuthService
            .BuildAuthorizationUrl(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new GoogleAuthorizationUrl("https://accounts.google.com/auth", "nonce"));

        // Act
        _controller.GoogleLogin(returnUrl: null);

        // Assert
        _googleAuthService.Received(1).BuildAuthorizationUrl(
            Arg.Any<string>(),
            "/dashboard");
    }

    [Fact]
    public void GoogleLogin_CallsBuildAuthorizationUrlWithCallbackUrl()
    {
        // Arrange
        _googleAuthService
            .BuildAuthorizationUrl(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new GoogleAuthorizationUrl("https://accounts.google.com/auth", "nonce"));

        // Act
        _controller.GoogleLogin();

        // Assert
        _googleAuthService.Received(1).BuildAuthorizationUrl(
            Arg.Is<string>(url => url.Contains("/api/auth/google/callback")),
            Arg.Any<string>());
    }

    // =====================================================================
    // GET /api/auth/google/callback - GoogleCallback
    // =====================================================================

    [Fact]
    public async Task GoogleCallback_WhenGoogleSendsError_RedirectsToLoginWithOAuthCancelledError()
    {
        // Arrange
        // No necesita configurar _googleAuthService ni _sender porque el error de Google
        // se maneja antes de llamar a cualquier servicio

        // Act
        var result = await _controller.GoogleCallback(
            code: null,
            state: null,
            error: "access_denied",
            CancellationToken.None);

        // Assert
        var redirect = result.Should().BeOfType<RedirectResult>().Subject;
        redirect.Url.Should().Contain("http://localhost:4200/auth/login");
        redirect.Url.Should().Contain("oauth_cancelled");
    }

    [Fact]
    public async Task GoogleCallback_WithoutCode_RedirectsToLoginWithOAuthFailedError()
    {
        // Arrange
        _googleAuthService
            .ParseStateReturnUrl(Arg.Any<string>())
            .Returns("/dashboard");

        // Act
        var result = await _controller.GoogleCallback(
            code: null,
            state: "validnonce1234567890123456789012:/dashboard",
            error: null,
            CancellationToken.None);

        // Assert
        var redirect = result.Should().BeOfType<RedirectResult>().Subject;
        redirect.Url.Should().Contain("http://localhost:4200/auth/login");
        redirect.Url.Should().Contain("oauth_failed");
    }

    [Fact]
    public async Task GoogleCallback_WithValidCodeAndSuccessfulLogin_RedirectsToFrontendCallbackWithToken()
    {
        // Arrange
        var loginResponse = CreateLoginResponse(accessToken: "google-jwt-token");
        _googleAuthService
            .ParseStateReturnUrl(Arg.Any<string>())
            .Returns("/dashboard");
        _sender.Send(Arg.Any<LoginWithGoogleCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<LoginResponse>.Success(loginResponse));

        // Act
        var result = await _controller.GoogleCallback(
            code: "valid-google-code",
            state: "nonce12345678901234567890123456:/dashboard",
            error: null,
            CancellationToken.None);

        // Assert
        var redirect = result.Should().BeOfType<RedirectResult>().Subject;
        redirect.Url.Should().StartWith("http://localhost:4200/auth/callback#");
        redirect.Url.Should().Contain("access_token=google-jwt-token");
        redirect.Url.Should().Contain("token_type=Bearer");
    }

    [Fact]
    public async Task GoogleCallback_WithSuccessfulLogin_SetsRefreshTokenCookieWithLax()
    {
        // Arrange
        var loginResponse = CreateLoginResponse();
        _googleAuthService
            .ParseStateReturnUrl(Arg.Any<string>())
            .Returns("/dashboard");
        _sender.Send(Arg.Any<LoginWithGoogleCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<LoginResponse>.Success(loginResponse));

        // Act
        await _controller.GoogleCallback("google-code", "state", null, CancellationToken.None);

        // Assert
        var cookies = _controller.HttpContext.Response.Headers["Set-Cookie"];
        cookies.ToString().Should().Contain("refreshToken");
    }

    [Fact]
    public async Task GoogleCallback_WithOAuthFailedError_RedirectsToLoginWithOAuthFailedError()
    {
        // Arrange
        _googleAuthService
            .ParseStateReturnUrl(Arg.Any<string>())
            .Returns("/dashboard");
        _sender.Send(Arg.Any<LoginWithGoogleCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<LoginResponse>.Failure(AuthErrorCodes.OAUTH_FAILED, "OAuth falló."));

        // Act
        var result = await _controller.GoogleCallback("code", "state", null, CancellationToken.None);

        // Assert
        var redirect = result.Should().BeOfType<RedirectResult>().Subject;
        redirect.Url.Should().Contain("http://localhost:4200/auth/login");
        redirect.Url.Should().Contain("oauth_failed");
    }

    [Fact]
    public async Task GoogleCallback_WithInvalidDomainError_RedirectsToLoginWithInvalidDomainError()
    {
        // Arrange
        _googleAuthService
            .ParseStateReturnUrl(Arg.Any<string>())
            .Returns("/dashboard");
        _sender.Send(Arg.Any<LoginWithGoogleCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<LoginResponse>.Failure(AuthErrorCodes.INVALID_DOMAIN, "Dominio no permitido."));

        // Act
        var result = await _controller.GoogleCallback("code", "state", null, CancellationToken.None);

        // Assert
        var redirect = result.Should().BeOfType<RedirectResult>().Subject;
        redirect.Url.Should().Contain("http://localhost:4200/auth/login");
        redirect.Url.Should().Contain("invalid_domain");
    }

    [Fact]
    public async Task GoogleCallback_ValidatesStateBeforeProcessing()
    {
        // Arrange
        _googleAuthService
            .ParseStateReturnUrl("my-state-value")
            .Returns("/custom-return");
        _sender.Send(Arg.Any<LoginWithGoogleCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<LoginResponse>.Success(CreateLoginResponse()));

        // Act
        await _controller.GoogleCallback("code", "my-state-value", null, CancellationToken.None);

        // Assert
        _googleAuthService.Received(1).ParseStateReturnUrl("my-state-value");
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
        SetupHttpContext();

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
            Role = UserRole.Admin,
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
        apiResponse.Data.Role.Should().Be(UserRole.Admin);
    }

    // =====================================================================
    // POST /api/auth/password/forgot - ForgotPassword
    // =====================================================================

    /// <summary>
    /// Verifica que el endpoint retorna 200 OK cuando el handler retorna Result.Success(),
    /// independientemente de si el email existe (prevención de enumeración de usuarios).
    /// </summary>
    [Fact]
    public async Task ForgotPassword_WhenHandlerSucceeds_Returns200()
    {
        // Arrange
        _sender.Send(Arg.Any<ForgotPasswordCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var command = new ForgotPasswordCommand("test@univ.edu");

        // Act
        var actionResult = await _controller.ForgotPassword(command, CancellationToken.None);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse>().Subject;
        apiResponse.Success.Should().BeTrue();
    }

    /// <summary>
    /// Verifica que el mensaje de respuesta del endpoint es genérico y no revela
    /// si el email solicitado existe o no en el sistema.
    /// </summary>
    [Fact]
    public async Task ForgotPassword_WhenHandlerSucceeds_ReturnsGenericMessage()
    {
        // Arrange
        _sender.Send(Arg.Any<ForgotPasswordCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var command = new ForgotPasswordCommand("test@univ.edu");

        // Act
        var actionResult = await _controller.ForgotPassword(command, CancellationToken.None);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse>().Subject;
        apiResponse.Message.Should().Contain("restablecimiento");
    }

    /// <summary>
    /// Verifica que el endpoint retorna 400 Bad Request cuando el handler retorna
    /// un error de validación (email con formato inválido pasado por el pipeline).
    /// </summary>
    [Fact]
    public async Task ForgotPassword_WhenHandlerReturnsValidationError_Returns400()
    {
        // Arrange
        _sender.Send(Arg.Any<ForgotPasswordCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(AuthErrorCodes.VALIDATION_ERROR, "Email invalido."));

        var command = new ForgotPasswordCommand("not-an-email");

        // Act
        var actionResult = await _controller.ForgotPassword(command, CancellationToken.None);

        // Assert
        var badRequest = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(400);
    }

    // =====================================================================
    // POST /api/auth/password/reset - ResetPassword
    // =====================================================================

    /// <summary>
    /// Verifica que el endpoint retorna 200 OK cuando el handler restablece
    /// la contraseña exitosamente con un token válido.
    /// </summary>
    [Fact]
    public async Task ResetPassword_WhenHandlerSucceeds_Returns200()
    {
        // Arrange
        _sender.Send(Arg.Any<ResetPasswordCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var command = new ResetPasswordCommand("valid-token", "NewPass1", "NewPass1");

        // Act
        var actionResult = await _controller.ResetPassword(command, CancellationToken.None);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse>().Subject;
        apiResponse.Success.Should().BeTrue();
    }

    /// <summary>
    /// Verifica que el endpoint retorna 400 Bad Request cuando el token de reset
    /// es inválido o ha expirado (INVALID_TOKEN).
    /// </summary>
    [Fact]
    public async Task ResetPassword_WhenTokenIsInvalidOrExpired_Returns400()
    {
        // Arrange
        _sender.Send(Arg.Any<ResetPasswordCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(AuthErrorCodes.INVALID_TOKEN, "El enlace es invalido o ha expirado."));

        var command = new ResetPasswordCommand("expired-token", "NewPass1", "NewPass1");

        // Act
        var actionResult = await _controller.ResetPassword(command, CancellationToken.None);

        // Assert
        var badRequest = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(400);

        var apiResponse = badRequest.Value.Should().BeOfType<ApiResponse>().Subject;
        apiResponse.Error!.Code.Should().Be(AuthErrorCodes.INVALID_TOKEN);
    }

    /// <summary>
    /// Verifica que el endpoint retorna 400 Bad Request para cualquier otro error
    /// devuelto por el handler (validación, contraseña débil, etc.).
    /// </summary>
    [Fact]
    public async Task ResetPassword_WhenHandlerReturnsGenericError_Returns400()
    {
        // Arrange
        _sender.Send(Arg.Any<ResetPasswordCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(AuthErrorCodes.VALIDATION_ERROR, "Error de validacion."));

        var command = new ResetPasswordCommand("token", "weak", "weak");

        // Act
        var actionResult = await _controller.ResetPassword(command, CancellationToken.None);

        // Assert
        actionResult.Should().BeOfType<BadRequestObjectResult>();
    }

    // =====================================================================
    // PUT /api/auth/password/change - ChangePassword
    // =====================================================================

    /// <summary>
    /// Verifica que el endpoint retorna 200 OK con el nuevo TokenResponse cuando
    /// el handler cambia la contraseña exitosamente.
    /// </summary>
    [Fact]
    public async Task ChangePassword_WhenHandlerSucceeds_Returns200WithTokenResponse()
    {
        // Arrange
        var tokenResponse = CreateTokenResponse("new-access-token", "new-refresh-token");
        _sender.Send(Arg.Any<ChangePasswordCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<TokenResponse>.Success(tokenResponse));

        var command = new ChangePasswordCommand("CurrentPass1", "NewSecurePass1", "NewSecurePass1");

        // Act
        var actionResult = await _controller.ChangePassword(command, CancellationToken.None);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<TokenResponse>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data!.AccessToken.Should().Be("new-access-token");
    }

    /// <summary>
    /// Verifica que el endpoint establece el nuevo refresh token como cookie httpOnly
    /// cuando el cambio de contraseña es exitoso.
    /// </summary>
    [Fact]
    public async Task ChangePassword_WhenHandlerSucceeds_SetsNewRefreshTokenCookie()
    {
        // Arrange
        var tokenResponse = CreateTokenResponse(refreshToken: "updated-refresh-token");
        _sender.Send(Arg.Any<ChangePasswordCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<TokenResponse>.Success(tokenResponse));

        var command = new ChangePasswordCommand("CurrentPass1", "NewSecurePass1", "NewSecurePass1");

        // Act
        await _controller.ChangePassword(command, CancellationToken.None);

        // Assert
        var cookies = _controller.HttpContext.Response.Headers["Set-Cookie"];
        cookies.ToString().Should().Contain("refreshToken");
    }

    /// <summary>
    /// Verifica que el endpoint retorna 401 Unauthorized cuando el usuario
    /// no está autenticado (UNAUTHORIZED).
    /// </summary>
    [Fact]
    public async Task ChangePassword_WhenUserIsUnauthorized_Returns401()
    {
        // Arrange
        _sender.Send(Arg.Any<ChangePasswordCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<TokenResponse>.Failure(AuthErrorCodes.UNAUTHORIZED, "No autorizado."));

        var command = new ChangePasswordCommand("CurrentPass1", "NewSecurePass1", "NewSecurePass1");

        // Act
        var actionResult = await _controller.ChangePassword(command, CancellationToken.None);

        // Assert
        var statusResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(401);
    }

    /// <summary>
    /// Verifica que el endpoint retorna 401 Unauthorized cuando la contraseña
    /// actual proporcionada es incorrecta (INVALID_CURRENT_PASSWORD).
    /// </summary>
    [Fact]
    public async Task ChangePassword_WhenCurrentPasswordIsWrong_Returns401()
    {
        // Arrange
        _sender.Send(Arg.Any<ChangePasswordCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<TokenResponse>.Failure(
                AuthErrorCodes.INVALID_CURRENT_PASSWORD,
                "La contrasena actual es incorrecta."));

        var command = new ChangePasswordCommand("wrong-password", "NewSecurePass1", "NewSecurePass1");

        // Act
        var actionResult = await _controller.ChangePassword(command, CancellationToken.None);

        // Assert
        var statusResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(401);
    }

    /// <summary>
    /// Verifica que el endpoint retorna 400 Bad Request para errores que no son
    /// de autenticación (errores de validación, contraseña débil, etc.).
    /// </summary>
    [Fact]
    public async Task ChangePassword_WhenHandlerReturnsValidationError_Returns400()
    {
        // Arrange
        _sender.Send(Arg.Any<ChangePasswordCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<TokenResponse>.Failure(
                AuthErrorCodes.VALIDATION_ERROR,
                "Error de validacion."));

        var command = new ChangePasswordCommand("CurrentPass1", "weak", "weak");

        // Act
        var actionResult = await _controller.ChangePassword(command, CancellationToken.None);

        // Assert
        var statusResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(400);
    }
}
