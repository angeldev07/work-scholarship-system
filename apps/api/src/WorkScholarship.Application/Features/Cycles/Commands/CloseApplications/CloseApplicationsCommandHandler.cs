using MediatR;
using Microsoft.EntityFrameworkCore;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Cycles.DTOs;

namespace WorkScholarship.Application.Features.Cycles.Commands.CloseApplications;

/// <summary>
/// Handler que procesa el comando para cerrar el período de postulaciones de un ciclo.
/// </summary>
/// <remarks>
/// Flujo de ejecución:
/// 1. Carga el ciclo por CycleId; retorna CYCLE_NOT_FOUND si no existe.
/// 2. Delega la validación de la transición al método de dominio Cycle.CloseApplications().
/// 3. Si la transición falla, traduce el DomainResult a Result con el código de error UPPER_SNAKE_CASE.
/// 4. Persiste el cambio de estado y retorna el CycleDto actualizado.
/// </remarks>
public class CloseApplicationsCommandHandler : IRequestHandler<CloseApplicationsCommand, Result<CycleDto>>
{
    private readonly IApplicationDbContext _context;

    /// <summary>
    /// Inicializa el handler con el contexto de base de datos.
    /// </summary>
    /// <param name="context">Contexto de base de datos de la aplicación.</param>
    public CloseApplicationsCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Procesa el comando de cierre de postulaciones aplicando las reglas de la máquina de estados.
    /// </summary>
    /// <param name="request">Comando con el CycleId del ciclo a cerrar postulaciones.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>
    /// Result con CycleDto actualizado en caso de éxito;
    /// Result.Failure con código de error en caso de estado inválido o ciclo no encontrado.
    /// </returns>
    /// <remarks>
    /// Códigos de error posibles:
    /// - CYCLE_NOT_FOUND: El ciclo solicitado no existe.
    /// - INVALID_TRANSITION: El ciclo no está en estado ApplicationsOpen.
    /// </remarks>
    public async Task<Result<CycleDto>> Handle(CloseApplicationsCommand request, CancellationToken cancellationToken)
    {
        var cycle = await _context.Cycles
            .FirstOrDefaultAsync(c => c.Id == request.CycleId, cancellationToken);

        if (cycle is null)
        {
            return Result<CycleDto>.Failure(
                $"{CycleAppError.CYCLE_NOT_FOUND}",
                "El ciclo solicitado no fue encontrado.");
        }

        var domainResult = cycle.CloseApplications();

        if (domainResult.IsFailure)
        {
            return Result<CycleDto>.Failure(
                domainResult.ErrorCodeString!,
                domainResult.ErrorMessage!);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var locationsCount = await _context.CycleLocations
            .CountAsync(cl => cl.CycleId == request.CycleId && cl.IsActive, cancellationToken);

        var supervisorsCount = await _context.SupervisorAssignments
            .CountAsync(sa => sa.CycleId == request.CycleId, cancellationToken);

        return Result<CycleDto>.Success(CycleDto.FromEntity(cycle, locationsCount, supervisorsCount));
    }
}
