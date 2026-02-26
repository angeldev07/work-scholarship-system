using WorkScholarship.Domain.Common;

namespace WorkScholarship.Domain.Events;

/// <summary>
/// Evento de dominio emitido cuando se cierra el período de postulaciones de un ciclo.
/// Puede disparar notificaciones a postulantes que no completaron su registro.
/// </summary>
/// <param name="CycleId">Identificador único del ciclo cuyas postulaciones se cerraron.</param>
/// <param name="OccurredOn">Fecha y hora en que ocurrió el evento en UTC.</param>
public record ApplicationsClosedEvent(Guid CycleId, DateTime OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Crea una nueva instancia del evento con la fecha actual en UTC.
    /// </summary>
    /// <param name="cycleId">Identificador único del ciclo cuyas postulaciones se cerraron.</param>
    public ApplicationsClosedEvent(Guid cycleId) : this(cycleId, DateTime.UtcNow) { }
}
