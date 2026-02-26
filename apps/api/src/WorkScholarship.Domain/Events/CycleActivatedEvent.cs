using WorkScholarship.Domain.Common;

namespace WorkScholarship.Domain.Events;

/// <summary>
/// Evento de dominio emitido cuando un ciclo pasa al estado Active tras confirmar la selección final.
/// Puede disparar notificaciones a becarios seleccionados y supervisores asignados.
/// </summary>
/// <param name="CycleId">Identificador único del ciclo que fue activado.</param>
/// <param name="OccurredOn">Fecha y hora en que ocurrió el evento en UTC.</param>
public record CycleActivatedEvent(Guid CycleId, DateTime OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Crea una nueva instancia del evento con la fecha actual en UTC.
    /// </summary>
    /// <param name="cycleId">Identificador único del ciclo que fue activado.</param>
    public CycleActivatedEvent(Guid cycleId) : this(cycleId, DateTime.UtcNow) { }
}
