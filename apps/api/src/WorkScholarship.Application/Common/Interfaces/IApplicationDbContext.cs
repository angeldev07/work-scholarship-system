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
    /// Guarda todos los cambios realizados en el contexto a la base de datos.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación para operaciones asíncronas.</param>
    /// <returns>El número de entradas escritas en la base de datos.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
