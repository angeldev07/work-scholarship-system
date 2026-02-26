namespace WorkScholarship.Domain.Enums;

/// <summary>
/// Define los estados posibles de un ciclo semestral del programa de becas trabajo.
/// La máquina de estados define las transiciones válidas entre estados.
/// </summary>
/// <remarks>
/// Diagrama de transiciones:
/// Configuration → ApplicationsOpen → ApplicationsClosed → Active → Closed
/// Desde ApplicationsClosed también se puede volver a ApplicationsOpen (ReopenApplications).
/// Desde Configuration, ApplicationsOpen y Active se pueden extender fechas (ExtendDates).
/// El estado Closed es terminal e inmutable — no hay retorno desde él.
/// </remarks>
public enum CycleStatus
{
    /// <summary>
    /// Estado inicial del ciclo. Permite configurar ubicaciones, horarios y supervisores.
    /// Las postulaciones no están disponibles para los estudiantes.
    /// </summary>
    Configuration = 0,

    /// <summary>
    /// Período de postulaciones abierto. Los postulantes pueden registrarse y completar su solicitud.
    /// </summary>
    ApplicationsOpen = 1,

    /// <summary>
    /// Postulaciones cerradas. Fase de revisión, entrevistas, evaluación y selección final.
    /// No se aceptan nuevas postulaciones, pero se puede reabrir si es necesario.
    /// </summary>
    ApplicationsClosed = 2,

    /// <summary>
    /// Ciclo en operación activa. Los becarios seleccionados trabajan y registran jornadas con evidencia fotográfica.
    /// </summary>
    Active = 3,

    /// <summary>
    /// Ciclo finalizado oficialmente. Los datos quedan congelados como snapshot histórico inmutable.
    /// Este es el estado terminal — no hay transición de regreso desde Closed.
    /// </summary>
    Closed = 4
}
