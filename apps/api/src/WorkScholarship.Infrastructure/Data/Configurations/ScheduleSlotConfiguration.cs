using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkScholarship.Domain.Entities;

namespace WorkScholarship.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad ScheduleSlot.
/// </summary>
/// <remarks>
/// ScheduleSlot define los horarios disponibles para una ubicación en un ciclo específico.
/// Configuraciones clave:
/// - FK a CycleLocation: cascade delete (al borrar CycleLocation, se borran sus slots)
/// - DayOfWeek almacenado como entero (1=Lunes ... 7=Domingo)
/// - StartTime y EndTime mapeados a TimeOnly con PostgreSQL usando conversión a TimeSpan
/// - Propiedades computadas DayOfWeekName y DurationInHours ignoradas
/// - Índice en CycleLocationId para consultas de horarios de una ubicación-ciclo
/// </remarks>
public class ScheduleSlotConfiguration : IEntityTypeConfiguration<ScheduleSlot>
{
    /// <summary>
    /// Configura el mapeo de la entidad ScheduleSlot a la tabla ScheduleSlots en PostgreSQL.
    /// </summary>
    /// <param name="builder">Constructor de configuración de la entidad.</param>
    public void Configure(EntityTypeBuilder<ScheduleSlot> builder)
    {
        builder.ToTable("ScheduleSlots");

        builder.HasKey(ss => ss.Id);

        builder.Property(ss => ss.Id)
            .ValueGeneratedNever();

        builder.Property(ss => ss.CycleLocationId)
            .IsRequired();

        builder.Property(ss => ss.DayOfWeek)
            .IsRequired();

        builder.Property(ss => ss.StartTime)
            .IsRequired()
            .HasColumnType("time");

        builder.Property(ss => ss.EndTime)
            .IsRequired()
            .HasColumnType("time");

        builder.Property(ss => ss.RequiredScholars)
            .IsRequired();

        builder.Property(ss => ss.CreatedAt)
            .IsRequired();

        builder.Property(ss => ss.UpdatedAt);

        builder.Property(ss => ss.CreatedBy)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(ss => ss.UpdatedBy)
            .HasMaxLength(256);

        // Índice en CycleLocationId para consultas de horarios de una ubicación-ciclo específica
        builder.HasIndex(ss => ss.CycleLocationId)
            .HasDatabaseName("IX_ScheduleSlots_CycleLocationId");

        // Ignorar propiedades computadas y eventos de dominio
        builder.Ignore(ss => ss.DomainEvents);
        builder.Ignore(ss => ss.DayOfWeekName);
        builder.Ignore(ss => ss.DurationInHours);
    }
}
