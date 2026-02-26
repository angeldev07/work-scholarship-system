using WorkScholarship.Domain.Common;

namespace WorkScholarship.Domain.Entities;

/// <summary>
/// Representa un slot de horario disponible para una ubicación en un ciclo específico.
/// Define los días y horas en que se requieren becarios en la ubicación.
/// </summary>
/// <remarks>
/// Los ScheduleSlots pertenecen a una CycleLocation específica, lo que significa que
/// los horarios son configurables por ciclo: la misma ubicación puede tener horarios
/// diferentes en distintos ciclos. Esta granularidad permite adaptarse a cambios
/// semestrales en los horarios del programa de becas.
///
/// Ejemplo: Sala de Lectura puede tener horario L-V 8:00-10:00 en "2024-1"
/// y L-V 7:00-9:00 en "2024-2".
/// </remarks>
public class ScheduleSlot : BaseEntity
{
    private ScheduleSlot() { }

    /// <summary>
    /// Crea un nuevo slot de horario para una ubicación en un ciclo.
    /// </summary>
    /// <param name="cycleLocationId">Identificador de la CycleLocation a la que pertenece este horario.</param>
    /// <param name="dayOfWeek">
    /// Día de la semana (1=Lunes, 2=Martes, ..., 5=Viernes, 6=Sábado, 7=Domingo).
    /// </param>
    /// <param name="startTime">Hora de inicio del turno.</param>
    /// <param name="endTime">Hora de fin del turno. Debe ser posterior a startTime.</param>
    /// <param name="requiredScholars">Número de becarios requeridos en este turno. Debe ser mayor a 0.</param>
    /// <param name="createdBy">Identificador del administrador que configura el horario.</param>
    /// <returns>Nueva instancia de ScheduleSlot.</returns>
    /// <exception cref="ArgumentException">Si cycleLocationId es Guid.Empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Si dayOfWeek está fuera de rango (1-7) o requiredScholars es 0.</exception>
    public static ScheduleSlot Create(
        Guid cycleLocationId,
        int dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int requiredScholars,
        string createdBy)
    {
        if (cycleLocationId == Guid.Empty)
            throw new ArgumentException("El identificador de la ubicación del ciclo es requerido.", nameof(cycleLocationId));

        if (dayOfWeek < 1 || dayOfWeek > 7)
            throw new ArgumentOutOfRangeException(nameof(dayOfWeek),
                "El día de la semana debe estar entre 1 (Lunes) y 7 (Domingo).");

        if (startTime >= endTime)
            throw new ArgumentException("La hora de inicio debe ser anterior a la hora de fin.", nameof(startTime));

        if (requiredScholars <= 0)
            throw new ArgumentOutOfRangeException(nameof(requiredScholars),
                "El número de becarios requeridos debe ser mayor a 0.");

        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("El identificador del creador es requerido.", nameof(createdBy));

        return new ScheduleSlot
        {
            CycleLocationId = cycleLocationId,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            RequiredScholars = requiredScholars,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }

    // =========================================================================
    // Propiedades
    // =========================================================================

    /// <summary>
    /// Identificador de la relación CycleLocation a la que pertenece este slot de horario.
    /// </summary>
    public Guid CycleLocationId { get; private set; }

    /// <summary>
    /// Día de la semana del turno (1=Lunes, 2=Martes, 3=Miércoles, 4=Jueves, 5=Viernes, 6=Sábado, 7=Domingo).
    /// </summary>
    public int DayOfWeek { get; private set; }

    /// <summary>
    /// Hora de inicio del turno en la ubicación.
    /// </summary>
    public TimeOnly StartTime { get; private set; }

    /// <summary>
    /// Hora de fin del turno en la ubicación.
    /// </summary>
    public TimeOnly EndTime { get; private set; }

    /// <summary>
    /// Número de becarios simultáneos requeridos en este turno.
    /// Define cuántos estudiantes deben estar presentes durante este horario.
    /// </summary>
    public int RequiredScholars { get; private set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// La relación CycleLocation a la que pertenece este slot de horario.
    /// </summary>
    public CycleLocation CycleLocation { get; private set; } = null!;

    // =========================================================================
    // Métodos de Comportamiento
    // =========================================================================

    /// <summary>
    /// Actualiza la configuración del slot de horario.
    /// </summary>
    /// <param name="startTime">Nueva hora de inicio.</param>
    /// <param name="endTime">Nueva hora de fin. Debe ser posterior a startTime.</param>
    /// <param name="requiredScholars">Nuevo número de becarios requeridos. Debe ser mayor a 0.</param>
    /// <param name="updatedBy">Identificador del administrador que actualiza el horario.</param>
    /// <exception cref="ArgumentException">Si startTime es posterior o igual a endTime.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Si requiredScholars es menor o igual a 0.</exception>
    public void Update(TimeOnly startTime, TimeOnly endTime, int requiredScholars, string updatedBy)
    {
        if (startTime >= endTime)
            throw new ArgumentException("La hora de inicio debe ser anterior a la hora de fin.", nameof(startTime));

        if (requiredScholars <= 0)
            throw new ArgumentOutOfRangeException(nameof(requiredScholars),
                "El número de becarios requeridos debe ser mayor a 0.");

        StartTime = startTime;
        EndTime = endTime;
        RequiredScholars = requiredScholars;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Calcula la duración del turno en horas.
    /// </summary>
    public double DurationInHours => (EndTime - StartTime).TotalHours;

    /// <summary>
    /// Nombre del día de la semana en español para presentación en UI.
    /// </summary>
    public string DayOfWeekName => DayOfWeek switch
    {
        1 => "Lunes",
        2 => "Martes",
        3 => "Miércoles",
        4 => "Jueves",
        5 => "Viernes",
        6 => "Sábado",
        7 => "Domingo",
        _ => "Desconocido"
    };
}
