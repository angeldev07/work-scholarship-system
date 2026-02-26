using Microsoft.EntityFrameworkCore;
using WorkScholarship.Domain.Entities;

namespace WorkScholarship.Application.Common.Interfaces;

/// <summary>
/// Interfaz del contexto de base de datos de la aplicación.
/// Define los DbSets disponibles y el método para guardar cambios.
/// </summary>
public interface IApplicationDbContext
{
    /// <summary>
    /// DbSet de usuarios del sistema.
    /// </summary>
    DbSet<User> Users { get; }

    /// <summary>
    /// DbSet de tokens de actualización.
    /// </summary>
    DbSet<RefreshToken> RefreshTokens { get; }

    /// <summary>
    /// DbSet de ciclos semestrales del programa de becas.
    /// </summary>
    DbSet<Cycle> Cycles { get; }

    /// <summary>
    /// DbSet del catálogo maestro de ubicaciones físicas (atemporal).
    /// </summary>
    DbSet<Location> Locations { get; }

    /// <summary>
    /// DbSet de relaciones Ciclo-Ubicación con configuración específica por ciclo.
    /// </summary>
    DbSet<CycleLocation> CycleLocations { get; }

    /// <summary>
    /// DbSet de asignaciones de supervisores a ubicaciones por ciclo.
    /// </summary>
    DbSet<SupervisorAssignment> SupervisorAssignments { get; }

    /// <summary>
    /// DbSet de slots de horario por relación CycleLocation.
    /// </summary>
    DbSet<ScheduleSlot> ScheduleSlots { get; }

    /// <summary>
    /// Guarda todos los cambios realizados en el contexto a la base de datos.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación para operaciones asíncronas.</param>
    /// <returns>El número de entradas escritas en la base de datos.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
