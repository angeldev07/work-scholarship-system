using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Domain.Interfaces;
using WorkScholarship.Infrastructure.Data;
using WorkScholarship.Infrastructure.Identity;
using WorkScholarship.Infrastructure.Services;

namespace WorkScholarship.Infrastructure;

/// <summary>
/// Clase de extensión para registrar servicios de la capa Infrastructure en DI.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra todos los servicios de Infrastructure: EF Core, servicios de identidad y dominio.
    /// </summary>
    /// <param name="services">Colección de servicios de DI.</param>
    /// <param name="configuration">Configuración de la aplicación para connection strings y settings.</param>
    /// <returns>La colección de servicios modificada para encadenamiento.</returns>
    /// <remarks>
    /// Registra:
    /// - ApplicationDbContext con Npgsql (PostgreSQL) y retry policy
    /// - IApplicationDbContext como interfaz para el contexto
    /// - ITokenService: Generación de JWT y refresh tokens
    /// - IPasswordHasher: Hashing de contraseñas con PBKDF2
    /// - ICurrentUserService: Extracción de claims del JWT actual
    /// - IDateTimeProvider: Abstracción de DateTime.UtcNow para testing
    /// </remarks>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            }));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // Identity services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Domain services
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        return services;
    }
}
