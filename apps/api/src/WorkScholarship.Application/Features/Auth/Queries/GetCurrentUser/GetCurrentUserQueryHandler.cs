using MediatR;
using Microsoft.EntityFrameworkCore;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Auth.Common;

namespace WorkScholarship.Application.Features.Auth.Queries.GetCurrentUser;

/// <summary>
/// Handler que procesa la query para obtener datos del usuario autenticado.
/// </summary>
/// <remarks>
/// Flujo:
/// 1. Verifica que haya un usuario autenticado (UserId en claims del JWT)
/// 2. Busca el usuario en la base de datos
/// 3. Verifica que la cuenta esté activa
/// 4. Retorna UserDto con los datos del usuario
/// </remarks>
public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// Inicializa el handler con las dependencias necesarias.
    /// </summary>
    /// <param name="context">Contexto de base de datos para acceder a Users.</param>
    /// <param name="currentUserService">Servicio para obtener el UserId del JWT actual.</param>
    public GetCurrentUserQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Procesa la query retornando los datos del usuario autenticado.
    /// </summary>
    /// <param name="request">Query sin parámetros (usa el UserId del JWT).</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>
    /// Result con UserDto si el usuario existe y está activo;
    /// Result.Failure con código de error específico si falla la validación.
    /// </returns>
    /// <remarks>
    /// Códigos de error posibles:
    /// - UNAUTHORIZED: No hay JWT válido o UserId no presente en claims
    /// - USER_NOT_FOUND: Usuario no encontrado en la base de datos
    /// - INACTIVE_ACCOUNT: Cuenta desactivada
    /// </remarks>
    public async Task<Result<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Result<UserDto>.Failure(
                AuthErrorCodes.UNAUTHORIZED,
                "No autorizado.");
        }

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _currentUserService.UserId.Value, cancellationToken);

        if (user is null)
        {
            return Result<UserDto>.Failure(
                AuthErrorCodes.USER_NOT_FOUND,
                "Usuario no encontrado.");
        }

        if (!user.IsActive)
        {
            return Result<UserDto>.Failure(
                AuthErrorCodes.INACTIVE_ACCOUNT,
                "Tu cuenta esta desactivada. Contacta al administrador.");
        }

        return Result<UserDto>.Success(UserDto.FromEntity(user));
    }
}
