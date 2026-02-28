using MediatR;
using Microsoft.EntityFrameworkCore;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Cycles.DTOs;

namespace WorkScholarship.Application.Features.Cycles.Commands.CloseCycle;

/// <summary>
/// Handler que procesa el comando para cerrar oficialmente un ciclo semestral.
/// </summary>
/// <remarks>
/// Flujo de ejecución:
/// 1. Carga el ciclo por CycleId; retorna CYCLE_NOT_FOUND si no existe.
/// 2. Delega la validación al método de dominio Cycle.Close() pasando los conteos de pendientes.
/// 3. Si la transición falla, traduce el DomainResult a Result con el código de error UPPER_SNAKE_CASE.
/// 4. Persiste el cambio de estado y retorna el CycleDto actualizado.
///
/// TODO RF-011 PRECONDICIONES PENDIENTES:
/// Cuando se implementen los subsistemas TRACK (RF-029-034) y DOC (RF-040-042),
/// este handler debe:
/// 1. Contar jornadas pendientes de aprobación y pasar como pendingShiftsCount
/// 2. Contar becarios sin bitácora generada y pasar como missingLogbooksCount
/// 3. Obtener el closedBy del usuario autenticado (actualmente hardcodeado como "admin")
/// Por ahora se pasan 0 para ambos counts para permitir el flujo básico de cierre.
/// </remarks>
public class CloseCycleCommandHandler : IRequestHandler<CloseCycleCommand, Result<CycleDto>>
{
    private readonly IApplicationDbContext _context;

    /// <summary>
    /// Inicializa el handler con el contexto de base de datos.
    /// </summary>
    /// <param name="context">Contexto de base de datos de la aplicación.</param>
    public CloseCycleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Procesa el comando de cierre del ciclo aplicando las reglas de la máquina de estados.
    /// </summary>
    /// <param name="request">Comando con el CycleId del ciclo a cerrar.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>
    /// Result con CycleDto actualizado en caso de éxito;
    /// Result.Failure con código de error en caso de estado inválido o ciclo no encontrado.
    /// </returns>
    /// <remarks>
    /// Códigos de error posibles:
    /// - CYCLE_NOT_FOUND: El ciclo solicitado no existe.
    /// - INVALID_TRANSITION: El ciclo no está en estado Active.
    /// - CYCLE_NOT_ENDED: La fecha actual es anterior a EndDate del ciclo.
    /// - PENDING_SHIFTS: Hay jornadas pendientes de aprobación (actualmente siempre 0).
    /// - MISSING_LOGBOOKS: Faltan bitácoras por generar (actualmente siempre 0).
    /// </remarks>
    public async Task<Result<CycleDto>> Handle(CloseCycleCommand request, CancellationToken cancellationToken)
    {
        var cycle = await _context.Cycles
            .FirstOrDefaultAsync(c => c.Id == request.CycleId, cancellationToken);

        if (cycle is null)
        {
            return Result<CycleDto>.Failure(
                $"{CycleAppError.CYCLE_NOT_FOUND}",
                "El ciclo solicitado no fue encontrado.");
        }

        // TODO RF-011: Reemplazar estos 0 con consultas reales a los subsistemas TRACK y DOC
        // cuando se implementen RF-029-034 y RF-040-042 respectivamente.
        // TODO RF-011: Obtener closedBy del usuario autenticado desde el contexto HTTP.
        var domainResult = cycle.Close(
            pendingShiftsCount: 0,
            missingLogbooksCount: 0,
            closedBy: "admin");

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
