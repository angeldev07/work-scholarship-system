namespace WorkScholarship.Domain.Enums;

/// <summary>
/// Códigos de error específicos de las operaciones del ciclo semestral.
/// Cada transición de estado y operación del ciclo retorna uno de estos códigos en caso de fallo.
/// </summary>
public enum CycleErrorCode
{
    /// <summary>
    /// La transición de estado solicitada no es válida desde el estado actual del ciclo.
    /// </summary>
    InvalidTransition,

    /// <summary>
    /// No hay ubicaciones activas configuradas para el ciclo.
    /// Requerido antes de abrir postulaciones.
    /// </summary>
    NoLocations,

    /// <summary>
    /// El total de becas disponibles es 0 o negativo.
    /// </summary>
    NoScholarships,

    /// <summary>
    /// El proceso de renovaciones no ha sido completado ni omitido.
    /// Requerido antes de abrir postulaciones en ciclos subsecuentes.
    /// </summary>
    RenewalsPending,

    /// <summary>
    /// La fecha de fin del ciclo aún no ha llegado.
    /// Requerido para poder cerrar el ciclo.
    /// </summary>
    CycleNotEnded,

    /// <summary>
    /// Existen jornadas laborales pendientes de aprobación por supervisores.
    /// Requerido: 0 pendientes para poder cerrar el ciclo.
    /// </summary>
    PendingShifts,

    /// <summary>
    /// Faltan bitácoras por generar para algunos becarios.
    /// Requerido: todas generadas para poder cerrar el ciclo.
    /// </summary>
    MissingLogbooks,

    /// <summary>
    /// El ciclo está cerrado y sus datos son inmutables.
    /// No se permite ninguna modificación.
    /// </summary>
    CycleClosed,

    /// <summary>
    /// La fecha proporcionada no cumple con las reglas de validación temporal.
    /// Puede ser: fecha menor a la actual, o que rompe la coherencia temporal entre fechas.
    /// </summary>
    InvalidDate
}
