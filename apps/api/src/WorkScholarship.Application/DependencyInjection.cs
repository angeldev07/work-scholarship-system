using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using WorkScholarship.Application.Common.Behaviors;

namespace WorkScholarship.Application;

/// <summary>
/// Clase de extensión para registrar servicios de la capa Application en DI.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra todos los servicios de Application: MediatR, FluentValidation y behaviors.
    /// </summary>
    /// <param name="services">Colección de servicios de DI.</param>
    /// <returns>La colección de servicios modificada para encadenamiento.</returns>
    /// <remarks>
    /// Registra:
    /// - MediatR: Handlers de Commands y Queries desde el assembly actual
    /// - FluentValidation: Validadores desde el assembly actual
    /// - ValidationBehavior: Pipeline behavior para ejecutar validaciones antes de handlers
    /// </remarks>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
        });

        services.AddValidatorsFromAssembly(assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
