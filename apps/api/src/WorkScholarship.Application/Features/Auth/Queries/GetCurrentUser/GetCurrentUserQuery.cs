using MediatR;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Auth.Common;

namespace WorkScholarship.Application.Features.Auth.Queries.GetCurrentUser;

/// <summary>
/// Query para obtener los datos del usuario actualmente autenticado.
/// </summary>
/// <remarks>
/// Extrae el UserId de los claims del JWT (vía ICurrentUserService) y
/// retorna los datos completos del usuario desde la base de datos.
/// Útil para que el frontend obtenga/valide el estado actual del usuario.
/// </remarks>
public record GetCurrentUserQuery : IRequest<Result<UserDto>>;
