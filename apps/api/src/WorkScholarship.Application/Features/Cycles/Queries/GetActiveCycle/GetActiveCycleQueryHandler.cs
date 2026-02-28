using MediatR;
using Microsoft.EntityFrameworkCore;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Cycles.DTOs;
using WorkScholarship.Domain.Enums;

namespace WorkScholarship.Application.Features.Cycles.Queries.GetActiveCycle;

/// <summary>
/// Handler que procesa la query para obtener el ciclo activo de un departamento.
/// </summary>
/// <remarks>
/// Retorna el ciclo más reciente que no esté en estado Closed para el departamento indicado.
/// Si no existe ninguno, retorna un Result exitoso con valor null (no es un error de negocio).
/// </remarks>
public class GetActiveCycleQueryHandler : IRequestHandler<GetActiveCycleQuery, Result<CycleDto?>>
{
    private readonly IApplicationDbContext _context;

    /// <summary>
    /// Inicializa el handler con el contexto de base de datos.
    /// </summary>
    /// <param name="context">Contexto de base de datos de la aplicación.</param>
    public GetActiveCycleQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Procesa la query retornando el ciclo no cerrado del departamento.
    /// </summary>
    /// <param name="request">Query con el nombre del departamento.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>
    /// Result exitoso con CycleDto si existe un ciclo activo;
    /// Result exitoso con null si no hay ciclos activos para el departamento.
    /// </returns>
    public async Task<Result<CycleDto?>> Handle(GetActiveCycleQuery request, CancellationToken cancellationToken)
    {
        var department = request.Department.Trim().ToLowerInvariant();

        var cycle = await _context.Cycles
            .AsNoTracking()
            .Where(c => c.Department.ToLower() == department && c.Status != CycleStatus.Closed)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (cycle is null)
        {
            return Result<CycleDto?>.Success(null);
        }

        var locationsCount = await _context.CycleLocations
            .CountAsync(cl => cl.CycleId == cycle.Id && cl.IsActive, cancellationToken);

        var supervisorsCount = await _context.SupervisorAssignments
            .CountAsync(sa => sa.CycleId == cycle.Id, cancellationToken);

        return Result<CycleDto?>.Success(CycleDto.FromEntity(cycle, locationsCount, supervisorsCount));
    }
}
