using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkScholarship.Domain.Entities;
using WorkScholarship.Domain.Enums;

namespace WorkScholarship.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad Cycle (ciclo semestral).
/// </summary>
/// <remarks>
/// Cycle es la entidad central del sistema. Todas las operaciones ocurren dentro del contexto de un ciclo.
/// Configuraciones clave:
/// - CycleStatus almacenado como string para legibilidad en base de datos
/// - Índice compuesto en (Department, Status) para validar la regla de negocio RN-001:
///   no puede haber más de un ciclo no-cerrado por departamento
/// - Navigation collections de CycleLocations y SupervisorAssignments configuradas con cascade
/// - DomainEvents ignorados (no se persisten)
/// </remarks>
public class CycleConfiguration : IEntityTypeConfiguration<Cycle>
{
    /// <summary>
    /// Configura el mapeo de la entidad Cycle a la tabla Cycles en PostgreSQL.
    /// </summary>
    /// <param name="builder">Constructor de configuración de la entidad.</param>
    public void Configure(EntityTypeBuilder<Cycle> builder)
    {
        builder.ToTable("Cycles");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedNever();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Department)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.StartDate)
            .IsRequired();

        builder.Property(c => c.EndDate)
            .IsRequired();

        builder.Property(c => c.ApplicationDeadline)
            .IsRequired();

        builder.Property(c => c.InterviewDate)
            .IsRequired();

        builder.Property(c => c.SelectionDate)
            .IsRequired();

        builder.Property(c => c.TotalScholarshipsAvailable)
            .IsRequired();

        builder.Property(c => c.TotalScholarshipsAssigned)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.ClosedAt);

        builder.Property(c => c.ClosedBy)
            .HasMaxLength(256);

        builder.Property(c => c.RenewalProcessCompleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.ClonedFromCycleId);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt);

        builder.Property(c => c.CreatedBy)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.UpdatedBy)
            .HasMaxLength(256);

        // Relación 1:N con CycleLocations
        builder.HasMany(c => c.CycleLocations)
            .WithOne(cl => cl.Cycle)
            .HasForeignKey(cl => cl.CycleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relación 1:N con SupervisorAssignments
        builder.HasMany(c => c.SupervisorAssignments)
            .WithOne(sa => sa.Cycle)
            .HasForeignKey(sa => sa.CycleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índice compuesto para validar RN-001: 1 ciclo no-cerrado por departamento
        // Permite consultas eficientes de ciclos activos por departamento
        builder.HasIndex(c => new { c.Department, c.Status })
            .HasDatabaseName("IX_Cycles_Department_Status");

        // Índice por Department para listar ciclos de una dependencia
        builder.HasIndex(c => c.Department)
            .HasDatabaseName("IX_Cycles_Department");

        // Ignorar propiedades computadas y eventos de dominio
        builder.Ignore(c => c.DomainEvents);
        builder.Ignore(c => c.IsModifiable);
        builder.Ignore(c => c.AcceptsApplications);
        builder.Ignore(c => c.IsOperational);
        builder.Ignore(c => c.IsClosed);
    }
}
