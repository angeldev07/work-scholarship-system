using System.Text.RegularExpressions;

namespace WorkScholarship.Application.Features.Admin.DTOs;

/// <summary>
/// Códigos tipados de acciones pendientes que el administrador debe completar.
/// Cada valor representa un prerequisito o configuración faltante en el departamento.
/// </summary>
public enum PendingActionCode
{
    /// <summary>
    /// El departamento no tiene ubicaciones activas en el catálogo maestro.
    /// </summary>
    NoLocations,

    /// <summary>
    /// El sistema no tiene supervisores activos registrados.
    /// </summary>
    NoSupervisors,

    /// <summary>
    /// No hay un ciclo activo ni en configuración para el departamento.
    /// </summary>
    NoActiveCycle,

    /// <summary>
    /// El ciclo en configuración no tiene ubicaciones asignadas.
    /// </summary>
    CycleNeedsLocations,

    /// <summary>
    /// El ciclo en configuración no tiene supervisores asignados.
    /// </summary>
    CycleNeedsSupervisors,

    /// <summary>
    /// El ciclo en configuración tiene renovaciones pendientes de procesar.
    /// </summary>
    RenewalsPending
}

/// <summary>
/// Representa una acción pendiente con código tipado (enum) y su representación
/// en string UPPER_SNAKE_CASE para serialización en respuestas API.
/// </summary>
/// <param name="Code">Código tipado de la acción pendiente.</param>
public partial record PendingActionItem(PendingActionCode Code)
{
    /// <summary>
    /// Código de la acción como string en formato UPPER_SNAKE_CASE.
    /// Traducido automáticamente desde el enum (ej: NoLocations → "NO_LOCATIONS").
    /// </summary>
    public string CodeString { get; } = ToUpperSnakeCase(Code.ToString());

    /// <summary>
    /// Convierte PascalCase a UPPER_SNAKE_CASE.
    /// </summary>
    private static string ToUpperSnakeCase(string pascalCase)
    {
        var result = UpperSnakeCaseRegex().Replace(pascalCase, "$1_$2");
        return result.ToUpperInvariant();
    }

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex UpperSnakeCaseRegex();
}
