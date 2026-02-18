namespace WorkScholarship.Domain.Interfaces;

/// <summary>
/// Proveedor de fecha y hora actual.
/// Permite abstraer DateTime.UtcNow para facilitar el testing.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// Obtiene la fecha y hora actual en UTC.
    /// </summary>
    DateTime UtcNow { get; }
}
