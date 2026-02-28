using MediatR;
using Microsoft.EntityFrameworkCore;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Cycles.DTOs;

namespace WorkScholarship.Application.Features.Cycles.Queries.ListCycles;

/// <summary>
/// Handler que procesa la query para obtener la lista paginada de ciclos semestrales.
/// </summary>
/// <remarks>
/// Aplica los filtros opcionales (departamento, año, estado) y retorna los resultados paginados
/// ordenados por fecha de creación descendente.
/// </remarks>
public class ListCyclesQueryHandler : IRequestHandler<ListCyclesQuery, Result<PaginatedList<CycleListItemDto>>>
{
    private readonly IApplicationDbContext _context;

    /// <summary>
    /// Inicializa el handler con el contexto de base de datos.
    /// </summary>
    /// <param name="context">Contexto de base de datos de la aplicación.</param>
    public ListCyclesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Procesa la query aplicando filtros y paginación sobre la colección de ciclos.
    /// </summary>
    /// <param name="request">Query con los filtros y parámetros de paginación.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>
    /// Result con la lista paginada de CycleListItemDto.
    /// Siempre es exitoso, incluso si no hay resultados (lista vacía).
    /// </returns>
    public async Task<Result<PaginatedList<CycleListItemDto>>> Handle(ListCyclesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Cycles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Department))
        {
            var dept = request.Department.Trim().ToLowerInvariant();
            query = query.Where(c => c.Department.ToLower() == dept);
        }

        if (request.Year.HasValue)
        {
            query = query.Where(c => c.StartDate.Year == request.Year.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(c => c.Status == request.Status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var cycles = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = cycles.Select(CycleListItemDto.FromEntity).ToList();

        var result = new PaginatedList<CycleListItemDto>(items, totalCount, request.Page, request.PageSize);

        return Result<PaginatedList<CycleListItemDto>>.Success(result);
    }
}
