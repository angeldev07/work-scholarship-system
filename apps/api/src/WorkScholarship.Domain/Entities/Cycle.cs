using WorkScholarship.Domain.Common;
using WorkScholarship.Domain.Enums;
using WorkScholarship.Domain.Events;

namespace WorkScholarship.Domain.Entities;

/// <summary>
/// Representa un ciclo semestral del programa de becas trabajo.
/// Es la entidad central del sistema: toda operación ocurre dentro del contexto de un ciclo.
/// Implementa una máquina de estados con 5 estados y transiciones controladas mediante Result pattern.
/// </summary>
/// <remarks>
/// El ciclo actúa como frontera temporal universal: postulaciones, selecciones,
/// jornadas laborales, ausencias y documentos pertenecen a un ciclo específico.
/// Al cerrar un ciclo, todos sus datos quedan congelados como snapshot histórico inmutable.
/// </remarks>
public class Cycle : BaseEntity
{
    private Cycle() { }

    /// <summary>
    /// Crea un nuevo ciclo semestral en estado Configuration.
    /// </summary>
    /// <param name="name">Nombre del ciclo (ej: "2024-1", "Enero-Mayo 2024").</param>
    /// <param name="department">Nombre de la dependencia o departamento universitario.</param>
    /// <param name="startDate">Fecha de inicio del período académico.</param>
    /// <param name="endDate">Fecha de fin del período académico. Debe ser posterior a startDate.</param>
    /// <param name="applicationDeadline">Fecha límite para recibir postulaciones. Debe ser anterior a interviewDate.</param>
    /// <param name="interviewDate">Fecha programada para entrevistas. Debe ser anterior a selectionDate.</param>
    /// <param name="selectionDate">Fecha de selección final. Debe ser anterior a endDate.</param>
    /// <param name="totalScholarshipsAvailable">Total de plazas de beca disponibles. Debe ser mayor a 0.</param>
    /// <param name="createdBy">Identificador del administrador que crea el ciclo.</param>
    /// <returns>Nueva instancia de Cycle en estado Configuration.</returns>
    /// <exception cref="ArgumentException">Si algún parámetro de texto requerido está vacío o es nulo.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Si las fechas no siguen el orden requerido o las becas son 0.</exception>
    public static Cycle Create(
        string name,
        string department,
        DateTime startDate,
        DateTime endDate,
        DateTime applicationDeadline,
        DateTime interviewDate,
        DateTime selectionDate,
        int totalScholarshipsAvailable,
        string createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del ciclo es requerido.", nameof(name));

        if (string.IsNullOrWhiteSpace(department))
            throw new ArgumentException("El departamento es requerido.", nameof(department));

        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("El identificador del creador es requerido.", nameof(createdBy));

        if (totalScholarshipsAvailable <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalScholarshipsAvailable),
                "El total de becas disponibles debe ser mayor a 0.");

        if (startDate >= endDate)
            throw new ArgumentException("La fecha de inicio debe ser anterior a la fecha de fin.");

        if (applicationDeadline >= interviewDate)
            throw new ArgumentException("La fecha límite de postulación debe ser anterior a la fecha de entrevistas.");

        if (interviewDate >= selectionDate)
            throw new ArgumentException("La fecha de entrevistas debe ser anterior a la fecha de selección.");

        if (selectionDate >= endDate)
            throw new ArgumentException("La fecha de selección debe ser anterior a la fecha de fin del ciclo.");

        var cycle = new Cycle
        {
            Name = name.Trim(),
            Department = department.Trim(),
            Status = CycleStatus.Configuration,
            StartDate = startDate,
            EndDate = endDate,
            ApplicationDeadline = applicationDeadline,
            InterviewDate = interviewDate,
            SelectionDate = selectionDate,
            TotalScholarshipsAvailable = totalScholarshipsAvailable,
            TotalScholarshipsAssigned = 0,
            RenewalProcessCompleted = false,
            ClonedFromCycleId = null,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        cycle.AddDomainEvent(new CycleCreatedEvent(cycle.Id));
        return cycle;
    }

    // =========================================================================
    // Propiedades
    // =========================================================================

    /// <summary>
    /// Nombre del ciclo semestral (ej: "2024-1", "Enero-Mayo 2024").
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Nombre de la dependencia o departamento universitario al que pertenece el ciclo.
    /// </summary>
    public string Department { get; private set; } = string.Empty;

    /// <summary>
    /// Estado actual del ciclo en la máquina de estados.
    /// </summary>
    public CycleStatus Status { get; private set; }

    /// <summary>
    /// Fecha de inicio del período académico.
    /// </summary>
    public DateTime StartDate { get; private set; }

    /// <summary>
    /// Fecha de fin del período académico.
    /// </summary>
    public DateTime EndDate { get; private set; }

    /// <summary>
    /// Fecha límite para recibir postulaciones de nuevos candidatos.
    /// </summary>
    public DateTime ApplicationDeadline { get; private set; }

    /// <summary>
    /// Fecha programada para la realización de entrevistas a postulantes.
    /// </summary>
    public DateTime InterviewDate { get; private set; }

    /// <summary>
    /// Fecha en que se realiza la selección final de becarios.
    /// </summary>
    public DateTime SelectionDate { get; private set; }

    /// <summary>
    /// Total de plazas de beca disponibles en este ciclo para el departamento.
    /// </summary>
    public int TotalScholarshipsAvailable { get; private set; }

    /// <summary>
    /// Total de plazas de beca actualmente asignadas a estudiantes en este ciclo.
    /// Se incrementa al confirmar asignaciones en el proceso de selección.
    /// </summary>
    public int TotalScholarshipsAssigned { get; private set; }

    /// <summary>
    /// Fecha y hora real en que se cerró el ciclo. Nulo si el ciclo no ha sido cerrado.
    /// </summary>
    public DateTime? ClosedAt { get; private set; }

    /// <summary>
    /// Identificador del administrador que cerró el ciclo. Nulo si el ciclo no ha sido cerrado.
    /// </summary>
    public string? ClosedBy { get; private set; }

    /// <summary>
    /// Indica si el proceso de renovaciones fue completado (o intencionalmente omitido) para este ciclo.
    /// Se marca automáticamente como true si es el primer ciclo de la dependencia (no hay renovables).
    /// Es el único flag de progreso persistido — el resto del progreso de configuración
    /// se calcula dinámicamente desde el estado actual de CycleLocations y SupervisorAssignments.
    /// </summary>
    public bool RenewalProcessCompleted { get; private set; }

    /// <summary>
    /// Identificador del ciclo del que se clonó la configuración (ubicaciones, supervisores, horarios).
    /// Nulo si la configuración fue establecida manualmente.
    /// </summary>
    public Guid? ClonedFromCycleId { get; private set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    private readonly List<CycleLocation> _cycleLocations = [];

    /// <summary>
    /// Colección de ubicaciones activas en este ciclo con su configuración específica.
    /// </summary>
    public IReadOnlyCollection<CycleLocation> CycleLocations => _cycleLocations.AsReadOnly();

    private readonly List<SupervisorAssignment> _supervisorAssignments = [];

    /// <summary>
    /// Colección de asignaciones de supervisores a ubicaciones en este ciclo.
    /// </summary>
    public IReadOnlyCollection<SupervisorAssignment> SupervisorAssignments => _supervisorAssignments.AsReadOnly();

    // =========================================================================
    // Transiciones de Estado
    // =========================================================================

    /// <summary>
    /// Abre el período de postulaciones del ciclo.
    /// Solo válido desde el estado Configuration.
    /// </summary>
    /// <param name="activeCycleLocationsCount">
    /// Cantidad de ubicaciones activas configuradas para este ciclo.
    /// Debe ser al menos 1 para poder abrir postulaciones.
    /// </param>
    /// <returns>
    /// DomainResult exitoso si la transición es válida; Failure con CycleErrorCode tipado en caso contrario.
    /// </returns>
    public DomainResult<CycleErrorCode> OpenApplications(int activeCycleLocationsCount)
    {
        if (Status != CycleStatus.Configuration)
            return DomainResult<CycleErrorCode>.Failure(CycleErrorCode.InvalidTransition,
                "Solo se puede abrir postulaciones desde el estado Configuration.");

        if (activeCycleLocationsCount == 0)
            return DomainResult<CycleErrorCode>.Failure(CycleErrorCode.NoLocations,
                "Debe haber al menos una ubicación activa configurada para abrir postulaciones.");

        if (TotalScholarshipsAvailable <= 0)
            return DomainResult<CycleErrorCode>.Failure(CycleErrorCode.NoScholarships,
                "El total de becas disponibles debe ser mayor a 0.");

        if (!RenewalProcessCompleted)
            return DomainResult<CycleErrorCode>.Failure(CycleErrorCode.RenewalsPending,
                "Debe procesar o saltar el proceso de renovaciones antes de abrir postulaciones.");

        Status = CycleStatus.ApplicationsOpen;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new ApplicationsOpenedEvent(Id));
        return DomainResult<CycleErrorCode>.Success();
    }

    /// <summary>
    /// Cierra el período de postulaciones del ciclo.
    /// Solo válido desde el estado ApplicationsOpen.
    /// </summary>
    /// <returns>
    /// DomainResult exitoso si la transición es válida; Failure con CycleErrorCode.InvalidTransition en caso contrario.
    /// </returns>
    public DomainResult<CycleErrorCode> CloseApplications()
    {
        if (Status != CycleStatus.ApplicationsOpen)
            return DomainResult<CycleErrorCode>.Failure(CycleErrorCode.InvalidTransition,
                "Solo se puede cerrar postulaciones desde el estado ApplicationsOpen.");

        Status = CycleStatus.ApplicationsClosed;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new ApplicationsClosedEvent(Id));
        return DomainResult<CycleErrorCode>.Success();
    }

    /// <summary>
    /// Reabre el período de postulaciones (válvula de escape).
    /// Solo válido desde el estado ApplicationsClosed.
    /// </summary>
    /// <returns>
    /// DomainResult exitoso si la transición es válida; Failure con CycleErrorCode.InvalidTransition en caso contrario.
    /// </returns>
    public DomainResult<CycleErrorCode> ReopenApplications()
    {
        if (Status != CycleStatus.ApplicationsClosed)
            return DomainResult<CycleErrorCode>.Failure(CycleErrorCode.InvalidTransition,
                "Solo se puede reabrir postulaciones desde el estado ApplicationsClosed.");

        Status = CycleStatus.ApplicationsOpen;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new ApplicationsReopenedEvent(Id));
        return DomainResult<CycleErrorCode>.Success();
    }

    /// <summary>
    /// Activa el ciclo tras confirmar la selección final de becarios.
    /// Solo válido desde el estado ApplicationsClosed.
    /// </summary>
    /// <returns>
    /// DomainResult exitoso si la transición es válida; Failure con CycleErrorCode.InvalidTransition en caso contrario.
    /// </returns>
    public DomainResult<CycleErrorCode> Activate()
    {
        if (Status != CycleStatus.ApplicationsClosed)
            return DomainResult<CycleErrorCode>.Failure(CycleErrorCode.InvalidTransition,
                "Solo se puede activar el ciclo desde el estado ApplicationsClosed.");

        Status = CycleStatus.Active;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new CycleActivatedEvent(Id));
        return DomainResult<CycleErrorCode>.Success();
    }

    /// <summary>
    /// Cierra oficialmente el ciclo. Los datos quedan congelados como snapshot histórico inmutable.
    /// Solo válido desde el estado Active.
    /// </summary>
    /// <param name="pendingShiftsCount">
    /// Cantidad de jornadas laborales pendientes de aprobación por supervisores.
    /// Debe ser 0 para poder cerrar el ciclo.
    /// </param>
    /// <param name="missingLogbooksCount">
    /// Cantidad de becarios sin bitácora generada.
    /// Debe ser 0 para poder cerrar el ciclo.
    /// </param>
    /// <param name="closedBy">Identificador del administrador que cierra el ciclo.</param>
    /// <returns>
    /// DomainResult exitoso si la transición es válida; Failure con CycleErrorCode tipado en caso contrario.
    /// </returns>
    public DomainResult<CycleErrorCode> Close(int pendingShiftsCount, int missingLogbooksCount, string closedBy)
    {
        if (Status != CycleStatus.Active)
            return DomainResult<CycleErrorCode>.Failure(CycleErrorCode.InvalidTransition,
                "Solo se puede cerrar el ciclo desde el estado Active.");

        if (DateTime.UtcNow < EndDate)
            return DomainResult<CycleErrorCode>.Failure(CycleErrorCode.CycleNotEnded,
                "No se puede cerrar el ciclo antes de la fecha de fin establecida.");

        if (pendingShiftsCount > 0)
            return DomainResult<CycleErrorCode>.Failure(CycleErrorCode.PendingShifts,
                $"Hay {pendingShiftsCount} jornadas pendientes de aprobación. Deben aprobarse antes de cerrar el ciclo.");

        if (missingLogbooksCount > 0)
            return DomainResult<CycleErrorCode>.Failure(CycleErrorCode.MissingLogbooks,
                $"Faltan bitácoras para {missingLogbooksCount} becarios. Deben generarse antes de cerrar el ciclo.");

        Status = CycleStatus.Closed;
        ClosedAt = DateTime.UtcNow;
        ClosedBy = closedBy;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new CycleClosedEvent(Id));
        return DomainResult<CycleErrorCode>.Success();
    }

    /// <summary>
    /// Extiende las fechas del ciclo. Solo permite extender (nunca reducir) fechas futuras.
    /// Válido desde los estados Configuration, ApplicationsOpen y Active.
    /// No válido en ApplicationsClosed (fase de entrevistas) ni Closed (inmutable).
    /// </summary>
    /// <param name="newApplicationDeadline">Nueva fecha límite de postulación (opcional). Debe ser mayor a la actual.</param>
    /// <param name="newInterviewDate">Nueva fecha de entrevistas (opcional). Debe ser mayor a la actual.</param>
    /// <param name="newSelectionDate">Nueva fecha de selección (opcional). Debe ser mayor a la actual.</param>
    /// <param name="newEndDate">Nueva fecha de fin del ciclo (opcional). Debe ser mayor a la actual.</param>
    /// <returns>
    /// DomainResult exitoso si la extensión es válida; Failure con CycleErrorCode tipado en caso contrario.
    /// </returns>
    public DomainResult<CycleErrorCode> ExtendDates(
        DateTime? newApplicationDeadline,
        DateTime? newInterviewDate,
        DateTime? newSelectionDate,
        DateTime? newEndDate)
    {
        if (Status == CycleStatus.Closed)
            return DomainResult<CycleErrorCode>.Failure(CycleErrorCode.CycleClosed,
                "No se pueden modificar las fechas de un ciclo cerrado.");

        if (Status == CycleStatus.ApplicationsClosed)
            return DomainResult<CycleErrorCode>.Failure(CycleErrorCode.InvalidTransition,
                "No se pueden extender fechas durante la fase de entrevistas (ApplicationsClosed).");

        // Validar que las nuevas fechas no sean menores que las actuales
        if (newApplicationDeadline.HasValue && newApplicationDeadline.Value <= ApplicationDeadline)
            return DomainResult<CycleErrorCode>.Failure(CycleErrorCode.InvalidDate,
                "La nueva fecha límite de postulación debe ser posterior a la fecha actual.");

        if (newInterviewDate.HasValue && newInterviewDate.Value <= InterviewDate)
            return DomainResult<CycleErrorCode>.Failure(CycleErrorCode.InvalidDate,
                "La nueva fecha de entrevistas debe ser posterior a la fecha actual.");

        if (newSelectionDate.HasValue && newSelectionDate.Value <= SelectionDate)
            return DomainResult<CycleErrorCode>.Failure(CycleErrorCode.InvalidDate,
                "La nueva fecha de selección debe ser posterior a la fecha actual.");

        if (newEndDate.HasValue && newEndDate.Value <= EndDate)
            return DomainResult<CycleErrorCode>.Failure(CycleErrorCode.InvalidDate,
                "La nueva fecha de fin debe ser posterior a la fecha actual.");

        // Calcular fechas finales para validar coherencia temporal
        var finalApplicationDeadline = newApplicationDeadline ?? ApplicationDeadline;
        var finalInterviewDate = newInterviewDate ?? InterviewDate;
        var finalSelectionDate = newSelectionDate ?? SelectionDate;
        var finalEndDate = newEndDate ?? EndDate;

        if (finalApplicationDeadline >= finalInterviewDate)
            return DomainResult<CycleErrorCode>.Failure(CycleErrorCode.InvalidDate,
                "La fecha límite de postulación debe ser anterior a la fecha de entrevistas.");

        if (finalInterviewDate >= finalSelectionDate)
            return DomainResult<CycleErrorCode>.Failure(CycleErrorCode.InvalidDate,
                "La fecha de entrevistas debe ser anterior a la fecha de selección.");

        if (finalSelectionDate >= finalEndDate)
            return DomainResult<CycleErrorCode>.Failure(CycleErrorCode.InvalidDate,
                "La fecha de selección debe ser anterior a la fecha de fin del ciclo.");

        // Aplicar cambios
        if (newApplicationDeadline.HasValue)
            ApplicationDeadline = newApplicationDeadline.Value;

        if (newInterviewDate.HasValue)
            InterviewDate = newInterviewDate.Value;

        if (newSelectionDate.HasValue)
            SelectionDate = newSelectionDate.Value;

        if (newEndDate.HasValue)
            EndDate = newEndDate.Value;

        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new CycleDatesExtendedEvent(Id));
        return DomainResult<CycleErrorCode>.Success();
    }

    // =========================================================================
    // Métodos de Configuración
    // =========================================================================

    /// <summary>
    /// Marca el proceso de renovaciones como completado para este ciclo.
    /// Debe llamarse cuando el administrador confirma o salta el proceso de renovaciones.
    /// </summary>
    public void MarkRenewalProcessCompleted()
    {
        RenewalProcessCompleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Establece el identificador del ciclo del que se clonó la configuración.
    /// </summary>
    /// <param name="sourceCycleId">Identificador del ciclo fuente de la clonación.</param>
    public void SetClonedFromCycleId(Guid sourceCycleId)
    {
        ClonedFromCycleId = sourceCycleId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Incrementa el contador de becas asignadas al confirmar una asignación.
    /// </summary>
    /// <param name="count">Cantidad de becas a agregar al conteo. Por defecto es 1.</param>
    public void IncrementAssignedScholarships(int count = 1)
    {
        TotalScholarshipsAssigned += count;
        UpdatedAt = DateTime.UtcNow;
    }

    // =========================================================================
    // Métodos de Consulta (Query Methods)
    // =========================================================================

    /// <summary>
    /// Indica si el ciclo está en un estado que permite modificaciones de configuración.
    /// Los ciclos cerrados son inmutables.
    /// </summary>
    public bool IsModifiable => Status != CycleStatus.Closed;

    /// <summary>
    /// Indica si el ciclo acepta nuevas postulaciones de candidatos.
    /// Solo cuando el estado es ApplicationsOpen.
    /// </summary>
    public bool AcceptsApplications => Status == CycleStatus.ApplicationsOpen;

    /// <summary>
    /// Indica si el ciclo está en operación activa con becarios trabajando.
    /// Solo cuando el estado es Active.
    /// </summary>
    public bool IsOperational => Status == CycleStatus.Active;

    /// <summary>
    /// Indica si el ciclo ya fue cerrado definitivamente.
    /// Los ciclos cerrados son snapshots históricos inmutables.
    /// </summary>
    public bool IsClosed => Status == CycleStatus.Closed;
}
