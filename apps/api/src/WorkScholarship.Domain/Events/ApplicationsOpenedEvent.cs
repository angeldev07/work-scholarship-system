using WorkScholarship.Domain.Common;

namespace WorkScholarship.Domain.Events;

/// <summary>
/// Evento de dominio emitido cuando se abre el período de postulaciones de un ciclo.
/// Puede disparar notificaciones a potenciales postulantes.
/// </summary>
/// <param name="CycleId">Identificador único del ciclo cuyas postulaciones se abrieron.</param>
/// <param name="OccurredOn">Fecha y hora en que ocurrió el evento en UTC.</param>
public record ApplicationsOpenedEvent(Guid CycleId, DateTime OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Crea una nueva instancia del evento con la fecha actual en UTC.
    /// </summary>
    /// <param name="cycleId">Identificador único del ciclo cuyas postulaciones se abrieron.</param>
    public ApplicationsOpenedEvent(Guid cycleId) : this(cycleId, DateTime.UtcNow) { }
}
