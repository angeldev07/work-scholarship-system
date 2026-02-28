namespace WorkScholarship.Application.Common.Models;

/// <summary>
/// Códigos de error de la capa Application para operaciones del módulo de ciclos.
/// Complementa los códigos del dominio (CycleErrorCode) con errores específicos de los casos de uso.
/// El string del código se obtiene con .ToString() ya que los valores siguen UPPER_SNAKE_CASE por convención.
/// </summary>
public enum CycleAppError
{
    /// <summary>
    /// Ya existe un ciclo no cerrado para este departamento (viola RN-001: 1 ciclo activo por dependencia).
    /// </summary>
    DUPLICATE_CYCLE,

    /// <summary>
    /// El ciclo solicitado no fue encontrado en la base de datos.
    /// </summary>
    CYCLE_NOT_FOUND,

    /// <summary>
    /// El ciclo fuente para clonación no es válido. Debe estar en estado Closed.
    /// </summary>
    INVALID_CLONE_SOURCE,

    /// <summary>
    /// El ciclo no está en estado Configuration. Solo se puede configurar en ese estado.
    /// </summary>
    NOT_IN_CONFIGURATION
}
