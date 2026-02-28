using MediatR;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Cycles.DTOs;

namespace WorkScholarship.Application.Features.Cycles.Commands.ConfigureCycle;

/// <summary>
/// DTO que representa la configuración de una ubicación dentro del comando de configuración de ciclo.
/// Incluye el número de becas disponibles, el estado activo y los slots de horario.
/// </summary>
public record CycleLocationInput
{
    /// <summary>
    /// Identificador de la ubicación maestra del catálogo.
    /// </summary>
    public Guid LocationId { get; init; }

    /// <summary>
    /// Número de plazas de beca disponibles en esta ubicación para el ciclo. Debe ser mayor a 0.
    /// </summary>
    public int ScholarshipsAvailable { get; init; }

    /// <summary>
    /// Indica si la ubicación está activa para este ciclo.
    /// </summary>
    public bool IsActive { get; init; } = true;

    /// <summary>
    /// Slots de horario configurados para esta ubicación en este ciclo.
    /// </summary>
    public List<ScheduleSlotInput> ScheduleSlots { get; init; } = [];
}

/// <summary>
/// DTO que representa un slot de horario dentro de la configuración de una ubicación.
/// </summary>
public record ScheduleSlotInput
{
    /// <summary>
    /// Día de la semana (1=Lunes, 2=Martes, ..., 5=Viernes, 6=Sábado, 7=Domingo).
    /// </summary>
    public int DayOfWeek { get; init; }

    /// <summary>
    /// Hora de inicio del turno en formato HH:mm (ej: "08:00").
    /// </summary>
    public TimeOnly StartTime { get; init; }

    /// <summary>
    /// Hora de fin del turno en formato HH:mm (ej: "10:00").
    /// </summary>
    public TimeOnly EndTime { get; init; }

    /// <summary>
    /// Número de becarios simultáneos requeridos en este turno. Debe ser mayor a 0.
    /// </summary>
    public int RequiredScholars { get; init; }
}

/// <summary>
/// DTO que representa la asignación de un supervisor a una ubicación dentro del comando de configuración.
/// </summary>
public record SupervisorAssignmentInput
{
    /// <summary>
    /// Identificador del usuario con rol Supervisor que se asigna a la ubicación.
    /// </summary>
    public Guid SupervisorId { get; init; }

    /// <summary>
    /// Identificador de la CycleLocation a la que se asigna el supervisor.
    /// </summary>
    public Guid CycleLocationId { get; init; }
}

/// <summary>
/// Comando para configurar un ciclo semestral (ubicaciones, supervisores y horarios).
/// Solo válido cuando el ciclo está en estado Configuration.
/// </summary>
/// <remarks>
/// Este comando realiza una operación de "upsert" completo:
/// - Procesa la lista de ubicaciones: crea nuevas CycleLocations si no existen,
///   actualiza las existentes y desactiva las que no aparezcan en el request.
/// - Reemplaza todos los ScheduleSlots de cada CycleLocation procesada.
/// - Reemplaza todas las SupervisorAssignments del ciclo con las provistas en el request.
/// - Recalcula el TotalScholarshipsAvailable del ciclo a partir de la suma de becas de las ubicaciones activas.
/// </remarks>
public record ConfigureCycleCommand : IRequest<Result<CycleDto>>
{
    /// <summary>
    /// Identificador del ciclo a configurar. Debe estar en estado Configuration.
    /// </summary>
    public Guid CycleId { get; init; }

    /// <summary>
    /// Lista de ubicaciones a configurar para el ciclo.
    /// Se realiza un "replace all": las ubicaciones no incluidas se desactivan.
    /// </summary>
    public List<CycleLocationInput> Locations { get; init; } = [];

    /// <summary>
    /// Lista de asignaciones de supervisores para el ciclo.
    /// Se realiza un "replace all": las asignaciones no incluidas se eliminan.
    /// </summary>
    public List<SupervisorAssignmentInput> SupervisorAssignments { get; init; } = [];
}
