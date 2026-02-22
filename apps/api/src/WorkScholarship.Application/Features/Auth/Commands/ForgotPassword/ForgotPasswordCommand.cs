using MediatR;
using WorkScholarship.Application.Common.Models;

namespace WorkScholarship.Application.Features.Auth.Commands.ForgotPassword;

/// <summary>
/// Comando para solicitar recuperación de contraseña por email.
/// </summary>
/// <param name="Email">Dirección de email del usuario que solicita el reset.</param>
/// <remarks>
/// Por seguridad, el handler SIEMPRE retorna éxito independientemente de si el email existe,
/// para evitar la enumeración de usuarios registrados.
/// </remarks>
public record ForgotPasswordCommand(string Email) : IRequest<Result>;
