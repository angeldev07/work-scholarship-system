using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkScholarship.Domain.Entities;

namespace WorkScholarship.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad SupervisorAssignment.
/// </summary>
/// <remarks>
/// SupervisorAssignment registra qué supervisor estaba asignado a qué ubicación en qué ciclo.
/// Es parte del snapshot histórico: al consultar un ciclo cerrado se puede ver exactamente
/// qué supervisores estaban asignados.
/// Configuraciones clave:
/// - FK a Cycle: cascade delete (al borrar ciclo, se borran sus asignaciones)
/// - FK a CycleLocation: Restrict para evitar cascada circular
/// - FK a User (Supervisor): Restrict (no borrar usuario con asignaciones históricas)
/// - Índices en CycleId y SupervisorId para consultas frecuentes
/// </remarks>
public class SupervisorAssignmentConfiguration : IEntityTypeConfiguration<SupervisorAssignment>
{
    /// <summary>
    /// Configura el mapeo de la entidad SupervisorAssignment a la tabla SupervisorAssignments en PostgreSQL.
    /// </summary>
    /// <param name="builder">Constructor de configuración de la entidad.</param>
    public void Configure(EntityTypeBuilder<SupervisorAssignment> builder)
    {
        builder.ToTable("SupervisorAssignments");

        builder.HasKey(sa => sa.Id);

        builder.Property(sa => sa.Id)
            .ValueGeneratedNever();

        builder.Property(sa => sa.CycleId)
            .IsRequired();

        builder.Property(sa => sa.CycleLocationId)
            .IsRequired();

        builder.Property(sa => sa.SupervisorId)
            .IsRequired();

        builder.Property(sa => sa.AssignedAt)
            .IsRequired();

        builder.Property(sa => sa.CreatedAt)
            .IsRequired();

        builder.Property(sa => sa.UpdatedAt);

        builder.Property(sa => sa.CreatedBy)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(sa => sa.UpdatedBy)
            .HasMaxLength(256);

        // Relación con User (Supervisor): Restrict — no borrar usuario con historial de asignaciones
        builder.HasOne(sa => sa.Supervisor)
            .WithMany()
            .HasForeignKey(sa => sa.SupervisorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índice en CycleId para consultas de supervisores de un ciclo
        builder.HasIndex(sa => sa.CycleId)
            .HasDatabaseName("IX_SupervisorAssignments_CycleId");

        // Índice en SupervisorId para consultas del historial de asignaciones de un supervisor
        builder.HasIndex(sa => sa.SupervisorId)
            .HasDatabaseName("IX_SupervisorAssignments_SupervisorId");

        // Índice en CycleLocationId para consultas de supervisores por ubicación-ciclo
        builder.HasIndex(sa => sa.CycleLocationId)
            .HasDatabaseName("IX_SupervisorAssignments_CycleLocationId");

        // Ignorar eventos de dominio
        builder.Ignore(sa => sa.DomainEvents);
    }
}
