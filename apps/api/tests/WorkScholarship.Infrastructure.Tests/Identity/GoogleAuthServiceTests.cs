using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Infrastructure.Identity;

namespace WorkScholarship.Infrastructure.Tests.Identity;

/// <summary>
/// Tests unitarios para GoogleAuthService.
/// Cubre BuildAuthorizationUrl, ParseStateReturnUrl y los comportamientos
/// de ExchangeCodeForUserInfoAsync con HttpClient mockeado.
/// </summary>
/// <remarks>
/// GoogleJsonWebSignature.ValidateAsync() es un método estático de Google.Apis.Auth
/// y no puede ser mockeado directamente. Los tests de ExchangeCodeForUserInfoAsync
/// cubren el comportamiento cuando:
/// - El token endpoint de Google devuelve error HTTP
/// - El token endpoint devuelve respuesta sin id_token
/// - El id_token es inválido (InvalidJwtException → retorna null)
/// - Hay error de red (HttpRequestException)
/// La validación exitosa de un id_token real solo se puede probar en tests de integración
/// con credenciales reales de Google Cloud Console.
/// </remarks>
[Trait("Category", "Infrastructure")]
[Trait("Feature", "Auth")]
[Trait("Component", "GoogleAuthService")]
public class GoogleAuthServiceTests
{
    private readonly GoogleAuthSettings _defaultSettings;

    public GoogleAuthServiceTests()
    {
        _defaultSettings = new GoogleAuthSettings
        {
            ClientId = "test-client-id.apps.googleusercontent.com",
            ClientSecret = "test-client-secret",
            AllowedDomains = [],
            FrontendUrl = "http://localhost:4200"
        };
    }

    /// <summary>
    /// Crea una instancia de GoogleAuthService con un HttpClient que usa el handler proporcionado.
    /// </summary>
    private GoogleAuthService CreateService(HttpMessageHandler? handler = null)
    {
        var httpClient = handler is not null
            ? new HttpClient(handler)
            : new HttpClient();

        var settings = Options.Create(_defaultSettings);
        var logger = NullLogger<GoogleAuthService>.Instance;

        return new GoogleAuthService(httpClient, settings, logger);
    }

    // =====================================================================
    // BuildAuthorizationUrl()
    // =====================================================================

    [Fact]
    public void BuildAuthorizationUrl_ReturnsUrlWithGoogleAuthEndpoint()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.BuildAuthorizationUrl(
            callbackUrl: "https://localhost:7001/api/auth/google/callback",
            returnUrl: "/dashboard");

        // Assert
        result.Url.Should().StartWith("https://accounts.google.com/o/oauth2/v2/auth");
    }

    [Fact]
    public void BuildAuthorizationUrl_ContainsClientId()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.BuildAuthorizationUrl(
            callbackUrl: "https://localhost:7001/api/auth/google/callback",
            returnUrl: "/dashboard");

        // Assert
        result.Url.Should().Contain("client_id=test-client-id.apps.googleusercontent.com");
    }

    [Fact]
    public void BuildAuthorizationUrl_ContainsRedirectUri()
    {
        // Arrange
        var service = CreateService();
        var callbackUrl = "https://localhost:7001/api/auth/google/callback";

        // Act
        var result = service.BuildAuthorizationUrl(callbackUrl, returnUrl: "/dashboard");

        // Assert
        result.Url.Should().Contain(Uri.EscapeDataString(callbackUrl));
    }

    [Fact]
    public void BuildAuthorizationUrl_ContainsRequiredOAuthScopes()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.BuildAuthorizationUrl(
            callbackUrl: "https://localhost:7001/api/auth/google/callback",
            returnUrl: "/dashboard");

        // Assert
        result.Url.Should().Contain("openid");
        result.Url.Should().Contain("email");
        result.Url.Should().Contain("profile");
    }

    [Fact]
    public void BuildAuthorizationUrl_ContainsResponseTypeCode()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.BuildAuthorizationUrl(
            callbackUrl: "https://localhost:7001/api/auth/google/callback",
            returnUrl: "/dashboard");

        // Assert
        result.Url.Should().Contain("response_type=code");
    }

    [Fact]
    public void BuildAuthorizationUrl_ReturnsCsrfNonceInResult()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.BuildAuthorizationUrl(
            callbackUrl: "https://localhost:7001/api/auth/google/callback",
            returnUrl: "/dashboard");

        // Assert
        result.CsrfNonce.Should().NotBeNullOrEmpty();
        result.CsrfNonce.Should().HaveLength(32); // GUID sin guiones = 32 chars hex
    }

    [Fact]
    public void BuildAuthorizationUrl_GeneratesUniqueCsrfNoncePerCall()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result1 = service.BuildAuthorizationUrl("https://callback.url", "/dashboard");
        var result2 = service.BuildAuthorizationUrl("https://callback.url", "/dashboard");

        // Assert
        result1.CsrfNonce.Should().NotBe(result2.CsrfNonce);
    }

    [Fact]
    public void BuildAuthorizationUrl_StateContainsCsrfNonce()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.BuildAuthorizationUrl(
            callbackUrl: "https://localhost:7001/api/auth/google/callback",
            returnUrl: "/dashboard");

        // Assert - el state en la URL debe contener el nonce generado
        result.Url.Should().Contain(result.CsrfNonce);
    }

    [Fact]
    public void BuildAuthorizationUrl_StateContainsReturnUrl()
    {
        // Arrange
        var service = CreateService();
        var returnUrl = "/admin/dashboard";

        // Act
        var result = service.BuildAuthorizationUrl(
            callbackUrl: "https://localhost:7001/api/auth/google/callback",
            returnUrl: returnUrl);

        // Assert - el returnUrl debe estar codificado en el state
        // (decodificamos la URL para verificar el state)
        var decodedUrl = Uri.UnescapeDataString(result.Url);
        decodedUrl.Should().Contain(returnUrl);
    }

    // =====================================================================
    // ParseStateReturnUrl()
    // =====================================================================

    [Fact]
    public void ParseStateReturnUrl_WithValidState_ReturnsReturnUrl()
    {
        // Arrange
        var service = CreateService();
        var nonce = Guid.NewGuid().ToString("N"); // 32 chars hex
        var returnUrl = "/dashboard";
        var state = Uri.EscapeDataString($"{nonce}:{returnUrl}");

        // Act
        var result = service.ParseStateReturnUrl(state);

        // Assert
        result.Should().Be(returnUrl);
    }

    [Fact]
    public void ParseStateReturnUrl_WithNullState_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.ParseStateReturnUrl(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ParseStateReturnUrl_WithEmptyState_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.ParseStateReturnUrl("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ParseStateReturnUrl_WithStateWithoutSeparator_ReturnsNull()
    {
        // Arrange
        var service = CreateService();
        var invalidState = Uri.EscapeDataString("noseparatorhere");

        // Act
        var result = service.ParseStateReturnUrl(invalidState);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ParseStateReturnUrl_WithInvalidNonce_ReturnsNull()
    {
        // Arrange
        var service = CreateService();
        // Nonce con caracteres inválidos (no hex) en el formato correcto
        var invalidState = Uri.EscapeDataString("NOTAHEXNONCE!@#$%^&*()12345:/dashboard");

        // Act
        var result = service.ParseStateReturnUrl(invalidState);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ParseStateReturnUrl_WithShortNonce_ReturnsNull()
    {
        // Arrange
        var service = CreateService();
        // Nonce demasiado corto (no tiene 32 chars)
        var invalidState = Uri.EscapeDataString("abc123:/dashboard");

        // Act
        var result = service.ParseStateReturnUrl(invalidState);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ParseStateReturnUrl_WithReturnUrlNotStartingWithSlash_ReturnsDefaultDashboard()
    {
        // Arrange
        var service = CreateService();
        var nonce = Guid.NewGuid().ToString("N");
        // ReturnUrl con URL absoluta (open redirect attempt)
        var state = Uri.EscapeDataString($"{nonce}:https://malicious.site.com");

        // Act
        var result = service.ParseStateReturnUrl(state);

        // Assert
        result.Should().Be("/dashboard");
    }

    [Fact]
    public void ParseStateReturnUrl_WithEmptyReturnUrl_ReturnsDefaultDashboard()
    {
        // Arrange
        var service = CreateService();
        var nonce = Guid.NewGuid().ToString("N");
        var state = Uri.EscapeDataString($"{nonce}:");

        // Act
        var result = service.ParseStateReturnUrl(state);

        // Assert
        result.Should().Be("/dashboard");
    }

    [Fact]
    public void ParseStateReturnUrl_WithComplexReturnUrl_PreservesReturnUrl()
    {
        // Arrange
        var service = CreateService();
        var nonce = Guid.NewGuid().ToString("N");
        var returnUrl = "/admin/users/123?tab=profile";
        var state = Uri.EscapeDataString($"{nonce}:{returnUrl}");

        // Act
        var result = service.ParseStateReturnUrl(state);

        // Assert
        result.Should().Be(returnUrl);
    }

    [Fact]
    public void ParseStateReturnUrl_RoundTripWithBuildAuthorizationUrl_ExtractsCorrectReturnUrl()
    {
        // Arrange
        var service = CreateService();
        var expectedReturnUrl = "/supervisor/jornadas";

        // Act - construir URL y luego extraer el state para parsearlo
        var authUrl = service.BuildAuthorizationUrl("https://callback.url", expectedReturnUrl);

        // Extraer el parámetro state de la query string usando Uri
        var uri = new Uri(authUrl.Url);
        var query = uri.Query.TrimStart('?');
        var stateParam = query
            .Split('&')
            .Select(p => p.Split('=', 2))
            .Where(parts => parts.Length == 2 && parts[0] == "state")
            .Select(parts => parts[1])
            .FirstOrDefault();

        var parsedReturnUrl = service.ParseStateReturnUrl(stateParam);

        // Assert
        parsedReturnUrl.Should().Be(expectedReturnUrl);
    }

    // =====================================================================
    // ExchangeCodeForUserInfoAsync() - Error paths (sin Google.Apis.Auth call)
    // =====================================================================

    [Fact]
    public async Task ExchangeCodeForUserInfo_WhenTokenEndpointReturnsBadRequest_ReturnsNull()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            statusCode: HttpStatusCode.BadRequest,
            content: "{\"error\": \"invalid_grant\", \"error_description\": \"Code was already redeemed.\"}");

        var service = CreateService(handler);

        // Act
        var result = await service.ExchangeCodeForUserInfoAsync(
            "invalid-or-expired-code",
            "https://localhost:7001/api/auth/google/callback");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExchangeCodeForUserInfo_WhenTokenEndpointReturnsUnauthorized_ReturnsNull()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            statusCode: HttpStatusCode.Unauthorized,
            content: "{\"error\": \"invalid_client\"}");

        var service = CreateService(handler);

        // Act
        var result = await service.ExchangeCodeForUserInfoAsync(
            "some-code",
            "https://localhost:7001/api/auth/google/callback");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExchangeCodeForUserInfo_WhenTokenResponseHasNoIdToken_ReturnsNull()
    {
        // Arrange
        var responseBody = JsonSerializer.Serialize(new
        {
            access_token = "some-access-token",
            token_type = "Bearer",
            expires_in = 3600
            // Sin id_token
        });

        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, responseBody);
        var service = CreateService(handler);

        // Act
        var result = await service.ExchangeCodeForUserInfoAsync(
            "valid-code",
            "https://localhost:7001/api/auth/google/callback");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExchangeCodeForUserInfo_WhenTokenResponseHasEmptyIdToken_ReturnsNull()
    {
        // Arrange
        var responseBody = JsonSerializer.Serialize(new
        {
            access_token = "some-access-token",
            id_token = "",
            token_type = "Bearer",
            expires_in = 3600
        });

        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, responseBody);
        var service = CreateService(handler);

        // Act
        var result = await service.ExchangeCodeForUserInfoAsync(
            "valid-code",
            "https://localhost:7001/api/auth/google/callback");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExchangeCodeForUserInfo_WhenIdTokenIsInvalidJwt_ReturnsNull()
    {
        // Arrange - id_token con formato inválido (Google.Apis.Auth lanzará InvalidJwtException)
        var responseBody = JsonSerializer.Serialize(new
        {
            access_token = "some-access-token",
            id_token = "this.is.not.a.valid.google.id.token",
            token_type = "Bearer",
            expires_in = 3600
        });

        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, responseBody);
        var service = CreateService(handler);

        // Act
        var result = await service.ExchangeCodeForUserInfoAsync(
            "some-code",
            "https://localhost:7001/api/auth/google/callback");

        // Assert - Google.Apis.Auth rechaza el token inválido → retorna null
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExchangeCodeForUserInfo_WhenTokenEndpointIsUnreachable_ReturnsNull()
    {
        // Arrange - handler que lanza HttpRequestException (simula falla de red)
        var handler = new FailingHttpMessageHandler(new HttpRequestException("Connection refused"));
        var service = CreateService(handler);

        // Act
        var result = await service.ExchangeCodeForUserInfoAsync(
            "some-code",
            "https://localhost:7001/api/auth/google/callback");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExchangeCodeForUserInfo_SendsCorrectParametersToTokenEndpoint()
    {
        // Arrange
        HttpMethod? capturedMethod = null;
        Uri? capturedUri = null;
        string? capturedBody = null;

        var handler = new CapturingHttpMessageHandler(
            onCapture: (method, uri, body) =>
            {
                capturedMethod = method;
                capturedUri = uri;
                capturedBody = body;
            },
            responseStatusCode: HttpStatusCode.BadRequest,
            responseContent: "{\"error\": \"invalid_grant\"}");

        var service = CreateService(handler);

        // Act
        await service.ExchangeCodeForUserInfoAsync(
            "my-auth-code",
            "https://localhost:7001/api/auth/google/callback");

        // Assert
        capturedMethod.Should().Be(HttpMethod.Post);
        capturedUri!.ToString().Should().Be("https://oauth2.googleapis.com/token");

        capturedBody.Should().Contain("code=my-auth-code");
        capturedBody.Should().Contain("client_id=test-client-id.apps.googleusercontent.com");
        capturedBody.Should().Contain("grant_type=authorization_code");
        capturedBody.Should().Contain(Uri.EscapeDataString("https://localhost:7001/api/auth/google/callback"));
    }

    // =====================================================================
    // Helpers de tests - Implementaciones falsas de HttpMessageHandler
    // =====================================================================

    /// <summary>
    /// Handler falso que retorna una respuesta HTTP predefinida.
    /// </summary>
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;

        public FakeHttpMessageHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content, Encoding.UTF8, "application/json")
            });
        }
    }

    /// <summary>
    /// Handler falso que siempre lanza la excepción especificada.
    /// </summary>
    private sealed class FailingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Exception _exception;

        public FailingHttpMessageHandler(Exception exception)
        {
            _exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            throw _exception;
        }
    }

    /// <summary>
    /// Handler falso que captura método, URI y cuerpo del request antes de retornar una respuesta predefinida.
    /// Almacena los valores capturados en el callback para que el test los pueda verificar.
    /// </summary>
    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Action<HttpMethod, Uri?, string?> _onCapture;
        private readonly HttpStatusCode _responseStatusCode;
        private readonly string _responseContent;

        public CapturingHttpMessageHandler(
            Action<HttpMethod, Uri?, string?> onCapture,
            HttpStatusCode responseStatusCode,
            string responseContent)
        {
            _onCapture = onCapture;
            _responseStatusCode = responseStatusCode;
            _responseContent = responseContent;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Leer y capturar el cuerpo antes de que se descarte el stream
            string? body = null;
            if (request.Content is not null)
            {
                body = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            _onCapture(request.Method, request.RequestUri, body);

            return new HttpResponseMessage(_responseStatusCode)
            {
                Content = new StringContent(_responseContent, Encoding.UTF8, "application/json")
            };
        }
    }
}
