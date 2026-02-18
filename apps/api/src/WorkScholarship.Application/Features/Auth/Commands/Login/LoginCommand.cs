using MediatR;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Auth.Common;

namespace WorkScholarship.Application.Features.Auth.Commands.Login;

/// <summary>
/// Comando para autenticar un usuario con email y contraseña (login local).
/// </summary>
/// <param name="Email">Dirección de correo electrónico del usuario.</param>
/// <param name="Password">Contraseña en texto plano del usuario.</param>
/// <remarks>
/// Valida las credenciales, verifica estado de la cuenta, genera JWT y refresh token.
/// Actualiza los contadores de login exitoso o fallido según corresponda.
/// </remarks>
public record LoginCommand(string Email, string Password) : IRequest<Result<LoginResponse>>;
