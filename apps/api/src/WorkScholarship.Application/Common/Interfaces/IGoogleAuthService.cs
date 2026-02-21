namespace WorkScholarship.Application.Common.Interfaces;

/// <summary>
/// Servicio para autenticación con Google OAuth 2.0.
/// Encapsula la construcción de URLs de autorización, el intercambio de authorization codes
/// y la validación criptográfica de id_tokens mediante Google.Apis.Auth.
/// </summary>
public interface IGoogleAuthService
{
    /// <summary>
    /// Construye la URL de autorización de Google OAuth 2.0 con protección CSRF.
    /// </summary>
    /// <param name="callbackUrl">URL de callback registrada en Google Cloud Console (ej: https://api.app.com/api/auth/google/callback).</param>
    /// <param name="returnUrl">URL del frontend a la que redirigir tras autenticación exitosa (codificada en el state).</param>
    /// <returns>
    /// <see cref="GoogleAuthorizationUrl"/> con la URL completa y el nonce CSRF generado.
    /// El nonce debe almacenarse temporalmente para validarlo en el callback.
    /// </returns>
    /// <remarks>
    /// El parámetro <c>state</c> de la URL contiene: <c>{nonce}:{returnUrl}</c>
    /// El nonce es un GUID aleatorio que previene ataques CSRF en el flujo OAuth.
    /// </remarks>
    GoogleAuthorizationUrl BuildAuthorizationUrl(string callbackUrl, string returnUrl);

    /// <summary>
    /// Valida el parámetro state del callback OAuth para prevenir ataques CSRF.
    /// </summary>
    /// <param name="state">Parámetro state recibido del callback de Google.</param>
    /// <returns>
    /// La <c>returnUrl</c> extraída del state si es válido;
    /// <c>null</c> si el state es inválido o malformado.
    /// </returns>
    /// <remarks>
    /// El formato esperado del state es: <c>{nonce}:{returnUrl}</c>
    /// El nonce debe ser un GUID válido. La returnUrl debe comenzar con '/'.
    /// </remarks>
    string? ParseStateReturnUrl(string? state);

    /// <summary>
    /// Intercambia un authorization code de Google por los datos del usuario autenticado.
    /// Valida criptográficamente el id_token usando <c>GoogleJsonWebSignature.ValidateAsync()</c>.
    /// </summary>
    /// <param name="authorizationCode">Código de autorización recibido del callback de Google.</param>
    /// <param name="redirectUri">URI de redirección usada en la solicitud original (debe coincidir exactamente).</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>
    /// Datos del usuario de Google si el intercambio y la validación fueron exitosos;
    /// <c>null</c> si el code es inválido, el id_token no pasa validación, o hay error de red.
    /// </returns>
    /// <remarks>
    /// Flujo interno:
    /// 1. POST al token endpoint de Google con el authorization code
    /// 2. Recibe id_token en la respuesta
    /// 3. Valida el id_token con GoogleJsonWebSignature.ValidateAsync() — verifica firma, audience, issuer y expiración
    /// 4. Extrae email, nombre, foto y sub (Google ID) del payload validado
    /// </remarks>
    Task<GoogleUserInfo?> ExchangeCodeForUserInfoAsync(
        string authorizationCode,
        string redirectUri,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Representa los datos de un usuario obtenidos de Google OAuth.
/// </summary>
/// <param name="Email">Dirección de correo electrónico del usuario de Google.</param>
/// <param name="FirstName">Nombre del usuario (given_name del token de Google).</param>
/// <param name="LastName">Apellido del usuario (family_name del token de Google).</param>
/// <param name="GoogleId">Identificador único del usuario en Google (sub claim).</param>
/// <param name="PhotoUrl">URL de la foto de perfil de Google (picture claim).</param>
public record GoogleUserInfo(
    string Email,
    string FirstName,
    string LastName,
    string GoogleId,
    string? PhotoUrl);

/// <summary>
/// Representa la URL de autorización de Google OAuth generada con protección CSRF.
/// </summary>
/// <param name="Url">URL completa del consent screen de Google con todos los parámetros OAuth.</param>
/// <param name="CsrfNonce">Nonce CSRF generado aleatoriamente e incluido en el parámetro state de la URL.</param>
public record GoogleAuthorizationUrl(string Url, string CsrfNonce);
