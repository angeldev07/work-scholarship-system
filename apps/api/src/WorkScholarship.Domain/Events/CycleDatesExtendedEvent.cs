using WorkScholarship.Domain.Common;

namespace WorkScholarship.Domain.Events;

/// <summary>
/// Evento de dominio emitido cuando se extienden las fechas de un ciclo.
/// Puede disparar notificaciones a usuarios afectados por el cambio de fechas.
/// </summary>
/// <param name="CycleId">Identificador único del ciclo cuyas fechas fueron extendidas.</param>
/// <param name="OccurredOn">Fecha y hora en que ocurrió el evento en UTC.</param>
public record CycleDatesExtendedEvent(Guid CycleId, DateTime OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Crea una nueva instancia del evento con la fecha actual en UTC.
    /// </summary>
    /// <param name="cycleId">Identificador único del ciclo cuyas fechas fueron extendidas.</param>
    public CycleDatesExtendedEvent(Guid cycleId) : this(cycleId, DateTime.UtcNow) { }
}
