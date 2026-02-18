using WorkScholarship.Domain.Interfaces;

namespace WorkScholarship.Infrastructure.Services;

/// <summary>
/// Implementación de IDateTimeProvider que retorna la fecha y hora actual del sistema.
/// </summary>
/// <remarks>
/// Abstrae DateTime.UtcNow para permitir testing con fechas/horas controladas.
/// En tests, se puede mockear IDateTimeProvider para retornar tiempos específicos.
/// </remarks>
public class DateTimeProvider : IDateTimeProvider
{
    /// <summary>
    /// Obtiene la fecha y hora actual en UTC.
    /// </summary>
    /// <value>DateTime.UtcNow del sistema.</value>
    public DateTime UtcNow => DateTime.UtcNow;
}
