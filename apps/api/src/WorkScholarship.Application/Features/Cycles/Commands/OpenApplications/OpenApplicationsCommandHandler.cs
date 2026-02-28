using MediatR;
using Microsoft.EntityFrameworkCore;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Cycles.DTOs;
using WorkScholarship.Domain.Enums;

namespace WorkScholarship.Application.Features.Cycles.Commands.OpenApplications;

/// <summary>
/// Handler que procesa el comando para abrir el período de postulaciones de un ciclo.
/// </summary>
/// <remarks>
/// Flujo de ejecución:
/// 1. Carga el ciclo por CycleId; retorna CYCLE_NOT_FOUND si no existe.
/// 2. Cuenta las CycleLocations activas del ciclo.
/// 3. Delega la validación de la transición al método de dominio Cycle.OpenApplications().
/// 4. Si la transición falla, traduce el DomainResult a Result con el código de error UPPER_SNAKE_CASE.
/// 5. Persiste el cambio de estado y retorna el CycleDto actualizado.
/// </remarks>
public class OpenApplicationsCommandHandler : IRequestHandler<OpenApplicationsCommand, Result<CycleDto>>
{
    private readonly IApplicationDbContext _context;

    /// <summary>
    /// Inicializa el handler con el contexto de base de datos.
    /// </summary>
    /// <param name="context">Contexto de base de datos de la aplicación.</param>
    public OpenApplicationsCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Procesa el comando de apertura de postulaciones aplicando las reglas de la máquina de estados.
    /// </summary>
    /// <param name="request">Comando con el CycleId del ciclo a abrir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>
    /// Result con CycleDto actualizado en caso de éxito;
    /// Result.Failure con código de error en caso de estado inválido o ciclo no encontrado.
    /// </returns>
    /// <remarks>
    /// Códigos de error posibles:
    /// - CYCLE_NOT_FOUND: El ciclo solicitado no existe.
    /// - INVALID_TRANSITION: El ciclo no está en estado Configuration.
    /// - NO_LOCATIONS: No hay ubicaciones activas configuradas para el ciclo.
    /// - NO_SCHOLARSHIPS: El total de becas disponibles es 0 o negativo.
    /// - RENEWALS_PENDING: El proceso de renovaciones no ha sido completado.
    /// </remarks>
    public async Task<Result<CycleDto>> Handle(OpenApplicationsCommand request, CancellationToken cancellationToken)
    {
        var cycle = await _context.Cycles
            .FirstOrDefaultAsync(c => c.Id == request.CycleId, cancellationToken);

        if (cycle is null)
        {
            return Result<CycleDto>.Failure(
                $"{CycleAppError.CYCLE_NOT_FOUND}",
                "El ciclo solicitado no fue encontrado.");
        }

        var activeLocationsCount = await _context.CycleLocations
            .CountAsync(cl => cl.CycleId == request.CycleId && cl.IsActive, cancellationToken);

        var domainResult = cycle.OpenApplications(activeLocationsCount);

        if (domainResult.IsFailure)
        {
            return Result<CycleDto>.Failure(
                domainResult.ErrorCodeString!,
                domainResult.ErrorMessage!);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var locationsCount = activeLocationsCount;
        var supervisorsCount = await _context.SupervisorAssignments
            .CountAsync(sa => sa.CycleId == request.CycleId, cancellationToken);

        return Result<CycleDto>.Success(CycleDto.FromEntity(cycle, locationsCount, supervisorsCount));
    }
}
