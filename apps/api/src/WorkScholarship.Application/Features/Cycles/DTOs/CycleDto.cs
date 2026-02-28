using WorkScholarship.Domain.Entities;
using WorkScholarship.Domain.Enums;

namespace WorkScholarship.Application.Features.Cycles.DTOs;

/// <summary>
/// DTO de resumen para un ciclo semestral del programa de becas.
/// Se utiliza en operaciones de creación y listado general.
/// </summary>
public record CycleDto
{
    /// <summary>
    /// Identificador único del ciclo.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Nombre del ciclo semestral (ej: "2024-1", "Enero-Mayo 2024").
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
    /// Fecha límite para recibir postulaciones de candidatos.
    /// </summary>
    public DateTime ApplicationDeadline { get; init; }

    /// <summary>
    /// Fecha programada para la realización de entrevistas.
    /// </summary>
    public DateTime InterviewDate { get; init; }

    /// <summary>
    /// Fecha en que se realiza la selección final de becarios.
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
    /// Cantidad de ubicaciones activas configuradas para este ciclo.
    /// </summary>
    public int LocationsCount { get; init; }

    /// <summary>
    /// Cantidad de supervisores asignados a este ciclo.
    /// </summary>
    public int SupervisorsCount { get; init; }

    /// <summary>
    /// Fecha y hora de creación del ciclo en UTC.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Fecha y hora de la última actualización del ciclo en UTC.
    /// </summary>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Crea un CycleDto a partir de la entidad de dominio Cycle.
    /// </summary>
    /// <param name="cycle">Entidad de dominio Cycle.</param>
    /// <param name="locationsCount">Cantidad de ubicaciones activas en el ciclo.</param>
    /// <param name="supervisorsCount">Cantidad de supervisores asignados al ciclo.</param>
    /// <returns>Instancia de CycleDto con los datos mapeados.</returns>
    public static CycleDto FromEntity(Cycle cycle, int locationsCount = 0, int supervisorsCount = 0) => new()
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
        LocationsCount = locationsCount,
        SupervisorsCount = supervisorsCount,
        CreatedAt = cycle.CreatedAt,
        UpdatedAt = cycle.UpdatedAt
    };
}
