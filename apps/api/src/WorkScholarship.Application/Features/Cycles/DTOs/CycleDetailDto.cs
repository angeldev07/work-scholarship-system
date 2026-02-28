using WorkScholarship.Domain.Entities;
using WorkScholarship.Domain.Enums;

namespace WorkScholarship.Application.Features.Cycles.DTOs;

/// <summary>
/// DTO de detalle completo para un ciclo semestral, incluyendo estadísticas de configuración.
/// Se utiliza en el endpoint GET /api/cycles/{id}.
/// </summary>
public record CycleDetailDto
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
    /// Nombre del departamento o dependencia universitaria propietaria del ciclo.
    /// </summary>
    public string Department { get; init; } = string.Empty;

    /// <summary>
    /// Estado actual del ciclo en la máquina de estados.
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
    /// Fecha límite para recibir postulaciones.
    /// </summary>
    public DateTime ApplicationDeadline { get; init; }

    /// <summary>
    /// Fecha programada para la realización de entrevistas.
    /// </summary>
    public DateTime InterviewDate { get; init; }

    /// <summary>
    /// Fecha de selección final de becarios.
    /// </summary>
    public DateTime SelectionDate { get; init; }

    /// <summary>
    /// Total de plazas de beca disponibles en el ciclo.
    /// </summary>
    public int TotalScholarshipsAvailable { get; init; }

    /// <summary>
    /// Total de plazas de beca actualmente asignadas a estudiantes.
    /// </summary>
    public int TotalScholarshipsAssigned { get; init; }

    /// <summary>
    /// Indica si el proceso de renovaciones fue completado o intencionalmente omitido.
    /// </summary>
    public bool RenewalProcessCompleted { get; init; }

    /// <summary>
    /// Identificador del ciclo del que se clonó la configuración. Nulo si fue manual.
    /// </summary>
    public Guid? ClonedFromCycleId { get; init; }

    /// <summary>
    /// Fecha y hora en que se cerró el ciclo. Nulo si aún no ha sido cerrado.
    /// </summary>
    public DateTime? ClosedAt { get; init; }

    /// <summary>
    /// Identificador del administrador que cerró el ciclo. Nulo si aún no ha sido cerrado.
    /// </summary>
    public string? ClosedBy { get; init; }

    /// <summary>
    /// Fecha y hora de creación del ciclo en UTC.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Identificador del administrador que creó el ciclo.
    /// </summary>
    public string CreatedBy { get; init; } = string.Empty;

    /// <summary>
    /// Fecha y hora de la última actualización del ciclo en UTC.
    /// </summary>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Cantidad de ubicaciones activas configuradas para este ciclo.
    /// </summary>
    public int LocationsCount { get; init; }

    /// <summary>
    /// Cantidad de supervisores asignados a este ciclo.
    /// </summary>
    public int SupervisorsCount { get; init; }

    /// <summary>
    /// Cantidad de becarios asignados a este ciclo (reservado para módulo de selección).
    /// </summary>
    public int ScholarsCount { get; init; }

    /// <summary>
    /// Crea un CycleDetailDto a partir de la entidad de dominio Cycle y sus contadores.
    /// </summary>
    /// <param name="cycle">Entidad de dominio Cycle.</param>
    /// <param name="locationsCount">Cantidad de ubicaciones activas en el ciclo.</param>
    /// <param name="supervisorsCount">Cantidad de supervisores asignados al ciclo.</param>
    /// <param name="scholarsCount">Cantidad de becarios asignados al ciclo.</param>
    /// <returns>Instancia de CycleDetailDto con los datos mapeados.</returns>
    public static CycleDetailDto FromEntity(
        Cycle cycle,
        int locationsCount = 0,
        int supervisorsCount = 0,
        int scholarsCount = 0) => new()
    {
        Id = cycle.Id,
        Name = cycle.Name,
        Department = cycle.Department,
        Status = cycle.Status,
        StartDate = cycle.StartDate,
        EndDate = cycle.EndDate,
        ApplicationDeadline = cycle.ApplicationDeadline,
        InterviewDate = cycle.InterviewDate,
        SelectionDate = cycle.SelectionDate,
        TotalScholarshipsAvailable = cycle.TotalScholarshipsAvailable,
        TotalScholarshipsAssigned = cycle.TotalScholarshipsAssigned,
        RenewalProcessCompleted = cycle.RenewalProcessCompleted,
        ClonedFromCycleId = cycle.ClonedFromCycleId,
        ClosedAt = cycle.ClosedAt,
        ClosedBy = cycle.ClosedBy,
        CreatedAt = cycle.CreatedAt,
        CreatedBy = cycle.CreatedBy,
        UpdatedAt = cycle.UpdatedAt,
        LocationsCount = locationsCount,
        SupervisorsCount = supervisorsCount,
        ScholarsCount = scholarsCount
    };
}
