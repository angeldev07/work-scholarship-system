using WorkScholarship.Domain.Common;

namespace WorkScholarship.Domain.Events;

/// <summary>
/// Evento de dominio emitido cuando se crea un nuevo ciclo semestral.
/// Se usa para disparar notificaciones y registrar el inicio del proceso de configuración.
/// </summary>
/// <param name="CycleId">Identificador único del ciclo creado.</param>
/// <param name="OccurredOn">Fecha y hora en que ocurrió el evento en UTC.</param>
public record CycleCreatedEvent(Guid CycleId, DateTime OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Crea una nueva instancia del evento con la fecha actual en UTC.
    /// </summary>
    /// <param name="cycleId">Identificador único del ciclo creado.</param>
    public CycleCreatedEvent(Guid cycleId) : this(cycleId, DateTime.UtcNow) { }
}
