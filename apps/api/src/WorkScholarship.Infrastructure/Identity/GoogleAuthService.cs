using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Common.Models;

namespace WorkScholarship.Infrastructure.Identity;

/// <summary>
/// Implementación de <see cref="IGoogleAuthService"/> que sigue el flujo OAuth 2.0 Authorization Code
/// de forma segura usando <c>Google.Apis.Auth</c> para validación criptográfica del id_token.
/// </summary>
/// <remarks>
/// Flujo implementado:
/// 1. <see cref="BuildAuthorizationUrl"/> construye la URL del consent screen con un nonce CSRF aleatorio en el state.
/// 2. <see cref="ParseStateReturnUrl"/> valida el state del callback y extrae la returnUrl.
/// 3. <see cref="ExchangeCodeForUserInfoAsync"/> intercambia el authorization code por tokens
///    y valida el id_token con <c>GoogleJsonWebSignature.ValidateAsync()</c>
///    que verifica: firma RSA de Google, audience (ClientId), issuer y expiración.
/// </remarks>
public class GoogleAuthService : IGoogleAuthService
{
    private const string GOOGLE_TOKEN_ENDPOINT = "https://oauth2.googleapis.com/token";
    private const string GOOGLE_AUTH_ENDPOINT = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string STATE_SEPARATOR = ":";

    private readonly HttpClient _httpClient;
    private readonly GoogleAuthSettings _settings;
    private readonly ILogger<GoogleAuthService> _logger;

    /// <summary>
    /// Inicializa el servicio con HttpClient, configuración de Google y logger.
    /// </summary>
    /// <param name="httpClient">HttpClient inyectado por IHttpClientFactory para llamadas al token endpoint de Google.</param>
    /// <param name="settings">Configuración de Google OAuth (ClientId, ClientSecret, AllowedDomains) desde appsettings.</param>
    /// <param name="logger">Logger para diagnósticos y errores.</param>
    public GoogleAuthService(
        HttpClient httpClient,
        IOptions<GoogleAuthSettings> settings,
        ILogger<GoogleAuthService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public GoogleAuthorizationUrl BuildAuthorizationUrl(string callbackUrl, string returnUrl)
    {
        // Generar nonce CSRF aleatorio — previene ataques de tipo Cross-Site Request Forgery
        var csrfNonce = Guid.NewGuid().ToString("N");

        // El state combina nonce + returnUrl para poder recuperar ambos en el callback
        // Formato: "{nonce}:{returnUrl}" — el separador ':' es seguro porque el nonce solo contiene hex
        var state = Uri.EscapeDataString($"{csrfNonce}{STATE_SEPARATOR}{returnUrl}");

        var url = GOOGLE_AUTH_ENDPOINT
            + $"?client_id={Uri.EscapeDataString(_settings.ClientId)}"
            + $"&redirect_uri={Uri.EscapeDataString(callbackUrl)}"
            + "&response_type=code"
            + "&scope=openid%20email%20profile"
            + "&access_type=offline"
            + "&prompt=consent"
            + $"&state={state}";

        _logger.LogDebug("URL de autorización de Google OAuth construida con nonce CSRF: {Nonce}", csrfNonce);

        return new GoogleAuthorizationUrl(Url: url, CsrfNonce: csrfNonce);
    }

    /// <inheritdoc />
    public string? ParseStateReturnUrl(string? state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            _logger.LogWarning("Callback de Google OAuth recibido sin parámetro state.");
            return null;
        }

        var decoded = Uri.UnescapeDataString(state);

        // El formato esperado es: "{nonce}:{returnUrl}"
        // El nonce es un GUID sin guiones (32 caracteres hex), seguido por ':' y la returnUrl
        var separatorIndex = decoded.IndexOf(STATE_SEPARATOR, StringComparison.Ordinal);

        if (separatorIndex < 0)
        {
            _logger.LogWarning("Parámetro state de OAuth inválido: no contiene separador. State: {State}", state);
            return null;
        }

        var noncePart = decoded[..separatorIndex];
        var returnUrlPart = decoded[(separatorIndex + 1)..];

        // Validar que el nonce tenga el formato correcto (GUID sin guiones = 32 chars hex)
        if (noncePart.Length != 32 || !IsHexString(noncePart))
        {
            _logger.LogWarning("Nonce CSRF inválido en state de OAuth. Nonce recibido: {Nonce}", noncePart);
            return null;
        }

        // Validar que la returnUrl sea relativa (comienza con '/') para prevenir open redirects
        if (string.IsNullOrWhiteSpace(returnUrlPart) || !returnUrlPart.StartsWith('/'))
        {
            _logger.LogWarning("ReturnUrl en state de OAuth es inválida: {ReturnUrl}", returnUrlPart);
            return "/dashboard";
        }

        return returnUrlPart;
    }

    /// <inheritdoc />
    public async Task<GoogleUserInfo?> ExchangeCodeForUserInfoAsync(
        string authorizationCode,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Paso 1: Intercambiar el authorization code por tokens de Google
            var idToken = await ExchangeCodeForIdTokenAsync(authorizationCode, redirectUri, cancellationToken);

            if (idToken is null)
            {
                return null;
            }

            // Paso 2: Validar el id_token criptográficamente con Google.Apis.Auth
            // ValidateAsync verifica: firma RSA, audience (ClientId), issuer (accounts.google.com) y expiración
            return await ValidateIdTokenAndExtractUserAsync(idToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de red durante el intercambio de OAuth code con Google.");
            return null;
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken != cancellationToken)
        {
            _logger.LogError(ex, "Timeout durante el intercambio de OAuth code con Google.");
            return null;
        }
    }

    /// <summary>
    /// Intercambia el authorization code de Google por el id_token mediante POST al token endpoint.
    /// </summary>
    /// <param name="authorizationCode">Authorization code recibido del callback de Google.</param>
    /// <param name="redirectUri">URI de redirección idéntica a la usada en la solicitud de autorización.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El id_token como string si el intercambio fue exitoso; null si falló.</returns>
    private async Task<string?> ExchangeCodeForIdTokenAsync(
        string authorizationCode,
        string redirectUri,
        CancellationToken cancellationToken)
    {
        var tokenRequest = new Dictionary<string, string>
        {
            ["code"] = authorizationCode,
            ["client_id"] = _settings.ClientId,
            ["client_secret"] = _settings.ClientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code"
        };

        var tokenResponse = await _httpClient.PostAsync(
            GOOGLE_TOKEN_ENDPOINT,
            new FormUrlEncodedContent(tokenRequest),
            cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var errorBody = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "El intercambio de authorization code con Google falló. Status: {StatusCode}. Detalle: {ErrorBody}",
                tokenResponse.StatusCode,
                errorBody);
            return null;
        }

        var tokenResult = await tokenResponse.Content
            .ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken: cancellationToken);

        if (tokenResult is null || string.IsNullOrWhiteSpace(tokenResult.IdToken))
        {
            _logger.LogWarning("Google retornó una respuesta sin id_token válido.");
            return null;
        }

        return tokenResult.IdToken;
    }

    /// <summary>
    /// Valida el id_token de Google usando <c>GoogleJsonWebSignature.ValidateAsync()</c>
    /// y extrae los datos del usuario del payload validado.
    /// </summary>
    /// <param name="idToken">JWT id_token recibido del token endpoint de Google.</param>
    /// <returns>
    /// <see cref="GoogleUserInfo"/> con datos del usuario si la validación es exitosa;
    /// null si el token es inválido, expirado o no pertenece a esta aplicación.
    /// </returns>
    /// <remarks>
    /// <c>GoogleJsonWebSignature.ValidateAsync()</c> verifica automáticamente:
    /// - Firma RSA con clave pública de Google (descargada y cacheada desde el endpoint JWKS)
    /// - Audience: debe coincidir con nuestro ClientId (previene token theft de otras apps)
    /// - Issuer: debe ser "accounts.google.com" o "https://accounts.google.com"
    /// - Expiración: el token no puede estar vencido
    /// </remarks>
    private async Task<GoogleUserInfo?> ValidateIdTokenAndExtractUserAsync(string idToken)
    {
        try
        {
            var validationSettings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [_settings.ClientId]
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);

            var email = payload.Email;
            var googleId = payload.Subject;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(googleId))
            {
                _logger.LogWarning("El id_token de Google no contiene email o sub requeridos.");
                return null;
            }

            var firstName = payload.GivenName ?? payload.Name ?? "Usuario";
            var lastName = payload.FamilyName ?? string.Empty;
            var photoUrl = payload.Picture;

            _logger.LogDebug(
                "id_token de Google validado exitosamente para el usuario: {Email} (GoogleId: {GoogleId})",
                email,
                googleId);

            return new GoogleUserInfo(
                Email: email,
                FirstName: firstName,
                LastName: lastName,
                GoogleId: googleId,
                PhotoUrl: photoUrl);
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(
                "Validación del id_token de Google falló. Token inválido o no pertenece a esta aplicación. Detalle: {Message}",
                ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al validar el id_token de Google.");
            return null;
        }
    }

    /// <summary>
    /// Verifica si un string contiene únicamente caracteres hexadecimales (0-9, a-f).
    /// </summary>
    /// <param name="value">String a verificar.</param>
    /// <returns>true si todos los caracteres son hexadecimales en minúscula; false en caso contrario.</returns>
    private static bool IsHexString(string value)
    {
        foreach (var c in value)
        {
            if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Modelo interno para deserializar la respuesta del token endpoint de Google.
    /// </summary>
    private sealed class GoogleTokenResponse
    {
        /// <summary>Access token de Google (no utilizado directamente en este flujo).</summary>
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }

        /// <summary>JWT firmado por Google con los datos del usuario autenticado.</summary>
        [JsonPropertyName("id_token")]
        public string? IdToken { get; init; }

        /// <summary>Refresh token de Google (opcional, solo presente si se solicitó offline access).</summary>
        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; init; }

        /// <summary>Tipo del token (normalmente "Bearer").</summary>
        [JsonPropertyName("token_type")]
        public string? TokenType { get; init; }

        /// <summary>Tiempo de expiración del access token en segundos.</summary>
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }
    }
}
