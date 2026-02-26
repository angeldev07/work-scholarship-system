using WorkScholarship.Domain.Common;

namespace WorkScholarship.Domain.Entities;

/// <summary>
/// Representa la asignación temporal de un supervisor a una ubicación en un ciclo específico.
/// Captura quién supervisó qué ubicación en qué período académico.
/// </summary>
/// <remarks>
/// Un supervisor puede estar asignado a múltiples ubicaciones en el mismo ciclo,
/// y la misma ubicación puede tener múltiples supervisores asignados.
/// Esta entidad es parte del snapshot histórico del ciclo: al consultar un ciclo cerrado,
/// se puede saber exactamente qué supervisores estaban asignados a cada ubicación.
/// </remarks>
public class SupervisorAssignment : BaseEntity
{
    private SupervisorAssignment() { }

    /// <summary>
    /// Crea una nueva asignación de supervisor a una ubicación en un ciclo.
    /// </summary>
    /// <param name="cycleId">Identificador del ciclo al que pertenece esta asignación.</param>
    /// <param name="cycleLocationId">Identificador de la CycleLocation (ubicación en el ciclo).</param>
    /// <param name="supervisorId">Identificador del usuario con rol Supervisor.</param>
    /// <param name="createdBy">Identificador del administrador que realiza la asignación.</param>
    /// <returns>Nueva instancia de SupervisorAssignment.</returns>
    /// <exception cref="ArgumentException">Si alguno de los identificadores es Guid.Empty o createdBy está vacío.</exception>
    public static SupervisorAssignment Create(
        Guid cycleId,
        Guid cycleLocationId,
        Guid supervisorId,
        string createdBy)
    {
        if (cycleId == Guid.Empty)
            throw new ArgumentException("El identificador del ciclo es requerido.", nameof(cycleId));

        if (cycleLocationId == Guid.Empty)
            throw new ArgumentException("El identificador de la ubicación del ciclo es requerido.", nameof(cycleLocationId));

        if (supervisorId == Guid.Empty)
            throw new ArgumentException("El identificador del supervisor es requerido.", nameof(supervisorId));

        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("El identificador del creador es requerido.", nameof(createdBy));

        return new SupervisorAssignment
        {
            CycleId = cycleId,
            CycleLocationId = cycleLocationId,
            SupervisorId = supervisorId,
            AssignedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }

    // =========================================================================
    // Propiedades
    // =========================================================================

    /// <summary>
    /// Identificador del ciclo al que pertenece esta asignación.
    /// </summary>
    public Guid CycleId { get; private set; }

    /// <summary>
    /// Identificador de la relación CycleLocation (ubicación específica en el ciclo).
    /// </summary>
    public Guid CycleLocationId { get; private set; }

    /// <summary>
    /// Identificador del usuario con rol Supervisor asignado a esta ubicación.
    /// </summary>
    public Guid SupervisorId { get; private set; }

    /// <summary>
    /// Fecha y hora en que se realizó la asignación del supervisor.
    /// </summary>
    public DateTime AssignedAt { get; private set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// El ciclo al que pertenece esta asignación.
    /// </summary>
    public Cycle Cycle { get; private set; } = null!;

    /// <summary>
    /// La relación ubicación-ciclo a la que está asignado este supervisor.
    /// </summary>
    public CycleLocation CycleLocation { get; private set; } = null!;

    /// <summary>
    /// El usuario con rol Supervisor asignado a esta ubicación en este ciclo.
    /// </summary>
    public User Supervisor { get; private set; } = null!;
}
