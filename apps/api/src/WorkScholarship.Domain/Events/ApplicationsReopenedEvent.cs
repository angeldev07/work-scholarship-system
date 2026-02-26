using WorkScholarship.Domain.Common;

namespace WorkScholarship.Domain.Events;

/// <summary>
/// Evento de dominio emitido cuando se reabre el período de postulaciones de un ciclo.
/// Funciona como válvula de escape en caso de errores en el cierre de postulaciones.
/// </summary>
/// <param name="CycleId">Identificador único del ciclo cuyas postulaciones se reabrieron.</param>
/// <param name="OccurredOn">Fecha y hora en que ocurrió el evento en UTC.</param>
public record ApplicationsReopenedEvent(Guid CycleId, DateTime OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Crea una nueva instancia del evento con la fecha actual en UTC.
    /// </summary>
    /// <param name="cycleId">Identificador único del ciclo cuyas postulaciones se reabrieron.</param>
    public ApplicationsReopenedEvent(Guid cycleId) : this(cycleId, DateTime.UtcNow) { }
}
