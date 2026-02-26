using WorkScholarship.Domain.Common;

namespace WorkScholarship.Domain.Entities;

/// <summary>
/// Representa la participación de una ubicación en un ciclo semestral específico.
/// Es la junction table entre Cycle y Location que captura la configuración temporal de cada ciclo.
/// </summary>
/// <remarks>
/// Razón de esta tabla: En el sistema predecesor, las ubicaciones no tenían scope de ciclo.
/// Esto impedía saber retrospectivamente qué ubicaciones estaban activas en un ciclo pasado,
/// ni cuántos becarios tenían asignados. CycleLocation resuelve esto: cada ciclo tiene su
/// propia "foto" de qué ubicaciones participaron y con qué configuración específica.
///
/// Cada CycleLocation puede tener:
/// - Múltiples ScheduleSlots que definen los horarios disponibles en ese ciclo
/// - Múltiples SupervisorAssignments para los supervisores asignados a esa ubicación en ese ciclo
/// </remarks>
public class CycleLocation : BaseEntity
{
    private CycleLocation() { }

    /// <summary>
    /// Crea una nueva relación entre una ubicación y un ciclo con su configuración específica.
    /// </summary>
    /// <param name="cycleId">Identificador del ciclo al que pertenece esta configuración.</param>
    /// <param name="locationId">Identificador de la ubicación maestra.</param>
    /// <param name="scholarshipsAvailable">
    /// Número de plazas de beca disponibles en esta ubicación para este ciclo.
    /// Debe ser mayor a 0.
    /// </param>
    /// <param name="createdBy">Identificador del administrador que configura la ubicación.</param>
    /// <returns>Nueva instancia de CycleLocation activa.</returns>
    /// <exception cref="ArgumentException">Si cycleId o locationId son Guid.Empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Si scholarshipsAvailable es menor o igual a 0.</exception>
    public static CycleLocation Create(
        Guid cycleId,
        Guid locationId,
        int scholarshipsAvailable,
        string createdBy)
    {
        if (cycleId == Guid.Empty)
            throw new ArgumentException("El identificador del ciclo es requerido.", nameof(cycleId));

        if (locationId == Guid.Empty)
            throw new ArgumentException("El identificador de la ubicación es requerido.", nameof(locationId));

        if (scholarshipsAvailable <= 0)
            throw new ArgumentOutOfRangeException(nameof(scholarshipsAvailable),
                "El número de becas disponibles debe ser mayor a 0.");

        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("El identificador del creador es requerido.", nameof(createdBy));

        return new CycleLocation
        {
            CycleId = cycleId,
            LocationId = locationId,
            ScholarshipsAvailable = scholarshipsAvailable,
            ScholarshipsAssigned = 0,
            IsActive = true,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }

    // =========================================================================
    // Propiedades
    // =========================================================================

    /// <summary>
    /// Identificador del ciclo al que pertenece esta configuración de ubicación.
    /// </summary>
    public Guid CycleId { get; private set; }

    /// <summary>
    /// Identificador de la ubicación maestra del catálogo.
    /// </summary>
    public Guid LocationId { get; private set; }

    /// <summary>
    /// Número de plazas de beca disponibles en esta ubicación para este ciclo específico.
    /// Puede variar entre ciclos para la misma ubicación.
    /// </summary>
    public int ScholarshipsAvailable { get; private set; }

    /// <summary>
    /// Número de plazas de beca actualmente asignadas a estudiantes en esta ubicación y ciclo.
    /// Se incrementa al confirmar asignaciones en el proceso de selección.
    /// </summary>
    public int ScholarshipsAssigned { get; private set; }

    /// <summary>
    /// Indica si esta ubicación está activa en el ciclo actual.
    /// Una ubicación puede desactivarse de un ciclo sin eliminar el registro histórico.
    /// </summary>
    public bool IsActive { get; private set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// El ciclo al que pertenece esta configuración.
    /// </summary>
    public Cycle Cycle { get; private set; } = null!;

    /// <summary>
    /// La ubicación maestra del catálogo que participa en este ciclo.
    /// </summary>
    public Location Location { get; private set; } = null!;

    private readonly List<ScheduleSlot> _scheduleSlots = [];

    /// <summary>
    /// Slots de horario configurados para esta ubicación en este ciclo.
    /// Define los días y horas en que se requieren becarios en esta ubicación.
    /// </summary>
    public IReadOnlyCollection<ScheduleSlot> ScheduleSlots => _scheduleSlots.AsReadOnly();

    private readonly List<SupervisorAssignment> _supervisorAssignments = [];

    /// <summary>
    /// Asignaciones de supervisores para esta ubicación en este ciclo.
    /// </summary>
    public IReadOnlyCollection<SupervisorAssignment> SupervisorAssignments => _supervisorAssignments.AsReadOnly();

    // =========================================================================
    // Métodos de Comportamiento
    // =========================================================================

    /// <summary>
    /// Actualiza el número de becas disponibles en esta ubicación para el ciclo.
    /// </summary>
    /// <param name="scholarshipsAvailable">Nuevo número de becas disponibles. Debe ser mayor a 0.</param>
    /// <param name="updatedBy">Identificador del administrador que realiza el cambio.</param>
    /// <exception cref="ArgumentOutOfRangeException">Si el valor es menor o igual a 0.</exception>
    public void UpdateScholarshipsAvailable(int scholarshipsAvailable, string updatedBy)
    {
        if (scholarshipsAvailable <= 0)
            throw new ArgumentOutOfRangeException(nameof(scholarshipsAvailable),
                "El número de becas disponibles debe ser mayor a 0.");

        ScholarshipsAvailable = scholarshipsAvailable;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Desactiva esta ubicación para el ciclo actual.
    /// La ubicación no recibirá asignaciones de becarios en este ciclo.
    /// </summary>
    /// <param name="updatedBy">Identificador del administrador que desactiva la ubicación del ciclo.</param>
    public void Deactivate(string updatedBy)
    {
        IsActive = false;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reactiva esta ubicación para el ciclo actual.
    /// </summary>
    /// <param name="updatedBy">Identificador del administrador que reactiva la ubicación del ciclo.</param>
    public void Activate(string updatedBy)
    {
        IsActive = true;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Incrementa el contador de becas asignadas al confirmar una asignación en esta ubicación.
    /// </summary>
    /// <param name="count">Cantidad de becas a agregar al conteo. Por defecto es 1.</param>
    public void IncrementAssignedScholarships(int count = 1)
    {
        ScholarshipsAssigned += count;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Indica si quedan plazas disponibles para asignar becarios en esta ubicación y ciclo.
    /// </summary>
    public bool HasAvailableSlots => ScholarshipsAvailable > ScholarshipsAssigned;

    /// <summary>
    /// Número de plazas restantes por asignar en esta ubicación para el ciclo.
    /// </summary>
    public int RemainingSlots => Math.Max(0, ScholarshipsAvailable - ScholarshipsAssigned);
}
