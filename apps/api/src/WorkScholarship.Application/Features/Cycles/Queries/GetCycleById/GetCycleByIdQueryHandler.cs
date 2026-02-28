using MediatR;
using Microsoft.EntityFrameworkCore;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Cycles.DTOs;

namespace WorkScholarship.Application.Features.Cycles.Queries.GetCycleById;

/// <summary>
/// Handler que procesa la query para obtener el detalle completo de un ciclo semestral.
/// </summary>
/// <remarks>
/// Flujo:
/// 1. Busca el ciclo por Id.
/// 2. Cuenta ubicaciones activas, supervisores asignados y becarios (placeholder = 0).
/// 3. Retorna CycleDetailDto con la información completa.
/// </remarks>
public class GetCycleByIdQueryHandler : IRequestHandler<GetCycleByIdQuery, Result<CycleDetailDto>>
{
    private readonly IApplicationDbContext _context;

    /// <summary>
    /// Inicializa el handler con el contexto de base de datos.
    /// </summary>
    /// <param name="context">Contexto de base de datos de la aplicación.</param>
    public GetCycleByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Procesa la query retornando el detalle del ciclo solicitado.
    /// </summary>
    /// <param name="request">Query con el Id del ciclo a consultar.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>
    /// Result con CycleDetailDto si el ciclo existe;
    /// Result.Failure con CYCLE_NOT_FOUND si no se encuentra.
    /// </returns>
    public async Task<Result<CycleDetailDto>> Handle(GetCycleByIdQuery request, CancellationToken cancellationToken)
    {
        var cycle = await _context.Cycles
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (cycle is null)
        {
            return Result<CycleDetailDto>.Failure(
                $"{CycleAppError.CYCLE_NOT_FOUND}",
                "El ciclo solicitado no fue encontrado.");
        }

        var locationsCount = await _context.CycleLocations
            .CountAsync(cl => cl.CycleId == cycle.Id && cl.IsActive, cancellationToken);

        var supervisorsCount = await _context.SupervisorAssignments
            .CountAsync(sa => sa.CycleId == cycle.Id, cancellationToken);

        // scholarsCount se expande cuando se implemente el módulo de selección (SEL)
        const int scholarsCount = 0;

        return Result<CycleDetailDto>.Success(
            CycleDetailDto.FromEntity(cycle, locationsCount, supervisorsCount, scholarsCount));
    }
}
