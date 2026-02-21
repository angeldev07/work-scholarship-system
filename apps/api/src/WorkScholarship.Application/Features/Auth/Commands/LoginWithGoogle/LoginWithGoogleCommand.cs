using MediatR;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Auth.Common;

namespace WorkScholarship.Application.Features.Auth.Commands.LoginWithGoogle;

/// <summary>
/// Comando para autenticar un usuario mediante Google OAuth 2.0.
/// </summary>
/// <param name="Code">Authorization code recibido del callback de Google OAuth.</param>
/// <param name="RedirectUri">URI de redirección usada en la solicitud original (debe coincidir con la registrada en Google).</param>
/// <remarks>
/// Flujo:
/// 1. Intercambia el code por datos del usuario de Google mediante IGoogleAuthService
/// 2. Valida dominio de email si está configurado (AllowedDomains en appsettings)
/// 3. Busca usuario existente por email
/// 4. Si no existe: crea nuevo usuario con User.CreateFromGoogle()
/// 5. Si existe con AuthProvider.Local: vincula cuenta Google con User.LinkGoogleAccount()
/// 6. Si existe con AuthProvider.Google: hace login directo
/// 7. Genera JWT access token + refresh token
/// </remarks>
public record LoginWithGoogleCommand(string Code, string RedirectUri) : IRequest<Result<LoginResponse>>;
