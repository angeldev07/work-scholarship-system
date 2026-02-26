using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkScholarship.Domain.Entities;

namespace WorkScholarship.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad Location (catálogo maestro de ubicaciones).
/// </summary>
/// <remarks>
/// Location es una entidad atemporal: existe independientemente de los ciclos.
/// Su relación con los ciclos se gestiona mediante CycleLocation (junction table).
/// Configuraciones clave:
/// - Índice en Department para consultar ubicaciones por dependencia
/// - IsActive con valor por defecto true
/// - Navigation collection CycleLocations ignorada en el mapeo raíz (configurada en CycleLocationConfiguration)
/// </remarks>
public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    /// <summary>
    /// Configura el mapeo de la entidad Location a la tabla Locations en PostgreSQL.
    /// </summary>
    /// <param name="builder">Constructor de configuración de la entidad.</param>
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Locations");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .ValueGeneratedNever();

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(l => l.Department)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.Description)
            .HasMaxLength(1000);

        builder.Property(l => l.Address)
            .HasMaxLength(500);

        builder.Property(l => l.ImageUrl)
            .HasMaxLength(2048);

        builder.Property(l => l.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(l => l.CreatedAt)
            .IsRequired();

        builder.Property(l => l.UpdatedAt);

        builder.Property(l => l.CreatedBy)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(l => l.UpdatedBy)
            .HasMaxLength(256);

        // Índice por departamento para consultas frecuentes de ubicaciones por dependencia
        builder.HasIndex(l => l.Department)
            .HasDatabaseName("IX_Locations_Department");

        // Índice compuesto para consultas de ubicaciones activas por departamento
        builder.HasIndex(l => new { l.Department, l.IsActive })
            .HasDatabaseName("IX_Locations_Department_IsActive");

        // Ignorar eventos de dominio (no se persisten en base de datos)
        builder.Ignore(l => l.DomainEvents);
    }
}
