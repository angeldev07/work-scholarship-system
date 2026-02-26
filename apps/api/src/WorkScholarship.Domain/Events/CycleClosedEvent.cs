using WorkScholarship.Domain.Common;

namespace WorkScholarship.Domain.Events;

/// <summary>
/// Evento de dominio emitido cuando se cierra oficialmente un ciclo semestral.
/// Al cerrarse, los datos del ciclo quedan congelados como snapshot histórico inmutable.
/// Puede disparar el cálculo de elegibilidad de renovación y notificaciones finales.
/// </summary>
/// <param name="CycleId">Identificador único del ciclo que fue cerrado.</param>
/// <param name="OccurredOn">Fecha y hora en que ocurrió el evento en UTC.</param>
public record CycleClosedEvent(Guid CycleId, DateTime OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Crea una nueva instancia del evento con la fecha actual en UTC.
    /// </summary>
    /// <param name="cycleId">Identificador único del ciclo que fue cerrado.</param>
    public CycleClosedEvent(Guid cycleId) : this(cycleId, DateTime.UtcNow) { }
}
