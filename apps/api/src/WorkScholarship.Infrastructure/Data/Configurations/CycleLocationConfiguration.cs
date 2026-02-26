using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkScholarship.Domain.Entities;

namespace WorkScholarship.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad CycleLocation (junction table Cycle-Location).
/// </summary>
/// <remarks>
/// CycleLocation captura la participación de una ubicación en un ciclo con su configuración específica.
/// Configuraciones clave:
/// - FK a Cycle (cascade delete: al borrar un ciclo, se borran sus CycleLocations)
/// - FK a Location (restrict: no se puede borrar una ubicación que participa en ciclos)
/// - Índice en CycleId para consultas frecuentes de ubicaciones de un ciclo
/// - Navigation collections de ScheduleSlots y SupervisorAssignments configuradas
/// - Propiedades computadas ignoradas (HasAvailableSlots, RemainingSlots)
/// </remarks>
public class CycleLocationConfiguration : IEntityTypeConfiguration<CycleLocation>
{
    /// <summary>
    /// Configura el mapeo de la entidad CycleLocation a la tabla CycleLocations en PostgreSQL.
    /// </summary>
    /// <param name="builder">Constructor de configuración de la entidad.</param>
    public void Configure(EntityTypeBuilder<CycleLocation> builder)
    {
        builder.ToTable("CycleLocations");

        builder.HasKey(cl => cl.Id);

        builder.Property(cl => cl.Id)
            .ValueGeneratedNever();

        builder.Property(cl => cl.CycleId)
            .IsRequired();

        builder.Property(cl => cl.LocationId)
            .IsRequired();

        builder.Property(cl => cl.ScholarshipsAvailable)
            .IsRequired();

        builder.Property(cl => cl.ScholarshipsAssigned)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(cl => cl.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(cl => cl.CreatedAt)
            .IsRequired();

        builder.Property(cl => cl.UpdatedAt);

        builder.Property(cl => cl.CreatedBy)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(cl => cl.UpdatedBy)
            .HasMaxLength(256);

        // Relación con Location: Restrict (no borrar ubicación con historial en ciclos)
        builder.HasOne(cl => cl.Location)
            .WithMany(l => l.CycleLocations)
            .HasForeignKey(cl => cl.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relación 1:N con ScheduleSlots (cascade delete)
        builder.HasMany(cl => cl.ScheduleSlots)
            .WithOne(ss => ss.CycleLocation)
            .HasForeignKey(ss => ss.CycleLocationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relación 1:N con SupervisorAssignments (cascade delete desde CycleLocation)
        // Nota: SupervisorAssignment también tiene FK a Cycle (cascade desde Cycle)
        builder.HasMany(cl => cl.SupervisorAssignments)
            .WithOne(sa => sa.CycleLocation)
            .HasForeignKey(sa => sa.CycleLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índice en CycleId para consultas de ubicaciones de un ciclo específico
        builder.HasIndex(cl => cl.CycleId)
            .HasDatabaseName("IX_CycleLocations_CycleId");

        // Índice compuesto para consultas de ubicaciones activas de un ciclo
        builder.HasIndex(cl => new { cl.CycleId, cl.IsActive })
            .HasDatabaseName("IX_CycleLocations_CycleId_IsActive");

        // Índice en LocationId para consultas de historial de una ubicación
        builder.HasIndex(cl => cl.LocationId)
            .HasDatabaseName("IX_CycleLocations_LocationId");

        // Ignorar propiedades computadas
        builder.Ignore(cl => cl.DomainEvents);
        builder.Ignore(cl => cl.HasAvailableSlots);
        builder.Ignore(cl => cl.RemainingSlots);
    }
}
