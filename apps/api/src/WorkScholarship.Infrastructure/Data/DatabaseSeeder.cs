using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Domain.Entities;
using WorkScholarship.Domain.Enums;

namespace WorkScholarship.Infrastructure.Data;

/// <summary>
/// Clase responsable de insertar datos iniciales de desarrollo en la base de datos.
/// </summary>
/// <remarks>
/// Solo debe ejecutarse en el entorno Development. Es idempotente: si los usuarios
/// ya existen por email, no se vuelven a crear. Usa el IPasswordHasher registrado
/// en DI para hashear contraseñas de forma segura.
/// </remarks>
public static class DatabaseSeeder
{
    /// <summary>
    /// Crea los usuarios de prueba iniciales si no existen en la base de datos.
    /// </summary>
    /// <param name="serviceProvider">Proveedor de servicios del contenedor DI.</param>
    /// <returns>Tarea asíncrona que representa la operación de seed.</returns>
    public static async Task SeedDevelopmentUsersAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILogger<ApplicationDbContext>>();

        logger.LogInformation("Iniciando seed de usuarios de desarrollo...");

        var usersToSeed = GetUsersToSeed();

        foreach (var (email, firstName, lastName, password, role) in usersToSeed)
        {
            var exists = await dbContext.Users
                .AnyAsync(u => u.Email == email.ToLowerInvariant());

            if (exists)
            {
                logger.LogInformation(
                    "Usuario {Email} ya existe, se omite la creación.",
                    email);
                continue;
            }

            var passwordHash = passwordHasher.Hash(password);

            var user = User.Create(
                email: email,
                firstName: firstName,
                lastName: lastName,
                passwordHash: passwordHash,
                role: role,
                createdBy: "system-seed");

            dbContext.Users.Add(user);

            logger.LogInformation(
                "Creando usuario de prueba: {Email} con rol {Role}.",
                email,
                role);
        }

        await dbContext.SaveChangesAsync();

        logger.LogInformation("Seed de usuarios de desarrollo completado.");
    }

    /// <summary>
    /// Define los usuarios de prueba que deben existir en desarrollo.
    /// </summary>
    /// <returns>Colección de tuplas con los datos de cada usuario a crear.</returns>
    private static IEnumerable<(string Email, string FirstName, string LastName, string Password, UserRole Role)>
        GetUsersToSeed()
    {
        return
        [
            (
                Email: "admin@test.com",
                FirstName: "Admin",
                LastName: "Sistema",
                Password: "Admin123!",
                Role: UserRole.Admin
            ),
            (
                Email: "supervisor@test.com",
                FirstName: "María",
                LastName: "García",
                Password: "Super123!",
                Role: UserRole.Supervisor
            ),
            (
                Email: "beca@test.com",
                FirstName: "Juan",
                LastName: "Pérez",
                Password: "Beca123!",
                Role: UserRole.Beca
            )
        ];
    }
}
