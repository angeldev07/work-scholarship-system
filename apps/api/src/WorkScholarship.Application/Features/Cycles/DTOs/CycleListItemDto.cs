using WorkScholarship.Domain.Entities;
using WorkScholarship.Domain.Enums;

namespace WorkScholarship.Application.Features.Cycles.DTOs;

/// <summary>
/// DTO de elemento de lista para el endpoint GET /api/cycles (lista paginada de ciclos).
/// Contiene solo la información esencial para mostrar en tablas y grids de administración.
/// </summary>
public record CycleListItemDto
{
    /// <summary>
    /// Identificador único del ciclo.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Nombre del ciclo semestral.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Nombre del departamento o dependencia universitaria.
    /// </summary>
    public string Department { get; init; } = string.Empty;

    /// <summary>
    /// Estado actual del ciclo.
    /// </summary>
    public CycleStatus Status { get; init; }

    /// <summary>
    /// Fecha de inicio del período académico.
    /// </summary>
    public DateTime StartDate { get; init; }

    /// <summary>
    /// Fecha de fin del período académico.
    /// </summary>
    public DateTime EndDate { get; init; }

    /// <summary>
    /// Total de plazas de beca disponibles.
    /// </summary>
    public int TotalScholarshipsAvailable { get; init; }

    /// <summary>
    /// Total de plazas de beca asignadas a estudiantes.
    /// </summary>
    public int TotalScholarshipsAssigned { get; init; }

    /// <summary>
    /// Fecha y hora de creación del ciclo en UTC.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Fecha y hora de cierre del ciclo. Nulo si no ha sido cerrado.
    /// </summary>
    public DateTime? ClosedAt { get; init; }

    /// <summary>
    /// Crea un CycleListItemDto a partir de la entidad de dominio Cycle.
    /// </summary>
    /// <param name="cycle">Entidad de dominio Cycle.</param>
    /// <returns>Instancia de CycleListItemDto con los datos mapeados.</returns>
    public static CycleListItemDto FromEntity(Cycle cycle) => new()
    {
        Id = cycle.Id,
        Name = cycle.Name,
        Department = cycle.Department,
        Status = cycle.Status,
        StartDate = cycle.StartDate,
        EndDate = cycle.EndDate,
        TotalScholarshipsAvailable = cycle.TotalScholarshipsAvailable,
        TotalScholarshipsAssigned = cycle.TotalScholarshipsAssigned,
        CreatedAt = cycle.CreatedAt,
        ClosedAt = cycle.ClosedAt
    };
}
