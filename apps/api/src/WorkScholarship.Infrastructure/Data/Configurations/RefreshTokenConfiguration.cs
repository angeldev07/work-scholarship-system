using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkScholarship.Domain.Entities;

namespace WorkScholarship.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad RefreshToken.
/// </summary>
/// <remarks>
/// Define schema de tabla, constraints e índices para PostgreSQL.
/// Configuraciones clave:
/// - Token único con índice (prevenir duplicados)
/// - Índice compuesto en UserId+ExpiresAt (para queries de limpieza de tokens expirados)
/// - Propiedades computed (IsExpired, IsRevoked, IsActive) ignoradas
/// - Propiedades de auditoría de BaseEntity ignoradas (no necesarias para tokens)
/// </remarks>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    /// <summary>
    /// Configura el mapeo de la entidad RefreshToken a la tabla RefreshTokens en PostgreSQL.
    /// </summary>
    /// <param name="builder">Constructor de configuración de la entidad.</param>
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Id)
            .ValueGeneratedNever();

        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(512);

        builder.HasIndex(rt => rt.Token)
            .IsUnique();

        builder.Property(rt => rt.UserId)
            .IsRequired();

        builder.Property(rt => rt.ExpiresAt)
            .IsRequired();

        builder.Property(rt => rt.RevokedAt);

        builder.Property(rt => rt.IpAddress)
            .HasMaxLength(45);

        builder.Property(rt => rt.CreatedAt)
            .IsRequired();

        builder.Ignore(rt => rt.IsExpired);
        builder.Ignore(rt => rt.IsRevoked);
        builder.Ignore(rt => rt.IsActive);

        // Ignore base entity properties not needed
        builder.Ignore(rt => rt.UpdatedAt);
        builder.Ignore(rt => rt.CreatedBy);
        builder.Ignore(rt => rt.UpdatedBy);
        builder.Ignore(rt => rt.DomainEvents);

        // Index for cleanup queries
        builder.HasIndex(rt => new { rt.UserId, rt.ExpiresAt });
    }
}
