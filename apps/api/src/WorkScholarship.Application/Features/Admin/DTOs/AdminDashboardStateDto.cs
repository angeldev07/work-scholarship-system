using WorkScholarship.Application.Features.Cycles.DTOs;

namespace WorkScholarship.Application.Features.Admin.DTOs;

/// <summary>
/// DTO con el estado completo del panel de administración para un departamento.
/// Proporciona toda la información necesaria para que el frontend determine qué vista mostrar
/// y qué acciones están disponibles.
/// </summary>
/// <remarks>
/// Este DTO es la "consulta de salud" del ciclo que reemplaza los flags persistidos de SetupCompleted.
/// El estado se calcula dinámicamente desde la base de datos en cada solicitud.
/// </remarks>
public record AdminDashboardStateDto
{
    /// <summary>
    /// Indica si el departamento tiene al menos una ubicación activa en el catálogo.
    /// </summary>
    public bool HasLocations { get; init; }

    /// <summary>
    /// Total de ubicaciones activas del departamento en el catálogo maestro.
    /// </summary>
    public int LocationsCount { get; init; }

    /// <summary>
    /// Indica si el sistema tiene al menos un supervisor activo.
    /// </summary>
    public bool HasSupervisors { get; init; }

    /// <summary>
    /// Total de usuarios con rol Supervisor activos en el sistema.
    /// </summary>
    public int SupervisorsCount { get; init; }

    /// <summary>
    /// El ciclo actualmente activo (estado Active) del departamento. Nulo si no existe.
    /// </summary>
    public CycleDto? ActiveCycle { get; init; }

    /// <summary>
    /// El ciclo más reciente en estado Closed del departamento. Nulo si no existe.
    /// Se usa para la opción "Clonar configuración del ciclo anterior" al crear uno nuevo.
    /// </summary>
    public CycleDto? LastClosedCycle { get; init; }

    /// <summary>
    /// El ciclo actualmente en configuración (estado Configuration, ApplicationsOpen o ApplicationsClosed).
    /// Nulo si no existe ninguno en estos estados.
    /// </summary>
    public CycleDto? CycleInConfiguration { get; init; }

    /// <summary>
    /// Lista de acciones pendientes que el administrador debe completar.
    /// Cada acción contiene un código tipado (enum) y su representación en UPPER_SNAKE_CASE.
    /// Guía al usuario hacia el siguiente paso necesario.
    /// </summary>
    public List<PendingActionItem> PendingActions { get; init; } = [];
}
