using MediatR;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Cycles.DTOs;
using WorkScholarship.Domain.Enums;

namespace WorkScholarship.Application.Features.Cycles.Queries.ListCycles;

/// <summary>
/// Query para obtener una lista paginada de ciclos semestrales con filtros opcionales.
/// </summary>
/// <remarks>
/// Soporta filtros por departamento, año, estado y paginación configurable.
/// Los ciclos se retornan ordenados por fecha de creación descendente (más reciente primero).
/// </remarks>
public record ListCyclesQuery : IRequest<Result<PaginatedList<CycleListItemDto>>>
{
    /// <summary>
    /// Filtro opcional por nombre de departamento. Búsqueda exacta (case-insensitive).
    /// </summary>
    public string? Department { get; init; }

    /// <summary>
    /// Filtro opcional por año del ciclo (basado en StartDate).
    /// </summary>
    public int? Year { get; init; }

    /// <summary>
    /// Filtro opcional por estado del ciclo.
    /// </summary>
    public CycleStatus? Status { get; init; }

    /// <summary>
    /// Número de página solicitada (base 1). Valor por defecto: 1.
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Cantidad de elementos por página. Valor por defecto: 10. Máximo recomendado: 50.
    /// </summary>
    public int PageSize { get; init; } = 10;
}
