using Microsoft.EntityFrameworkCore;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Domain.Entities;

namespace WorkScholarship.Infrastructure.Data;

/// <summary>
/// Contexto de base de datos principal de la aplicación usando EF Core.
/// </summary>
/// <remarks>
/// Implementa IApplicationDbContext para que la capa Application pueda acceder
/// a las entidades sin depender directamente de EF Core.
/// Aplica configuraciones de entidades automáticamente desde el assembly actual.
/// </remarks>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    /// <summary>
    /// Inicializa el contexto con las opciones de configuración.
    /// </summary>
    /// <param name="options">Opciones de configuración del contexto (connection string, provider, etc.).</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// DbSet para acceder a la tabla de usuarios.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// DbSet para acceder a la tabla de refresh tokens.
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>
    /// Configura el modelo de datos aplicando las configuraciones de entidades.
    /// </summary>
    /// <param name="modelBuilder">Constructor del modelo de EF Core.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
