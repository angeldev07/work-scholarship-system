namespace WorkScholarship.Domain.Common;

/// <summary>
/// Interfaz base para todos los eventos de dominio.
/// Los eventos de dominio representan algo importante que ocurrió en el dominio.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Fecha y hora en que ocurrió el evento en UTC.
    /// </summary>
    DateTime OccurredOn { get; }
}
