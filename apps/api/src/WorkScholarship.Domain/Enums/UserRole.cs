namespace WorkScholarship.Domain.Enums;

/// <summary>
/// Define los roles de usuario en el sistema de becas trabajo.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Sin rol asignado. Estado temporal durante el proceso de selección.
    /// </summary>
    None = 0,

    /// <summary>
    /// Administrador del sistema.
    /// </summary>
    /// <remarks>
    /// Tiene acceso total: crear ciclos, configurar ubicaciones, gestionar proceso de selección,
    /// asignar becas, generar documentos oficiales y revisar reportes globales.
    /// </remarks>
    Admin = 1,

    /// <summary>
    /// Supervisor o encargado de zona.
    /// </summary>
    /// <remarks>
    /// Responsable de aprobar jornadas, gestionar ausencias, supervisar becas asignados a su zona
    /// y firmar bitácoras digitales. Tiene acceso limitado a sus ubicaciones asignadas.
    /// </remarks>
    Supervisor = 2,

    /// <summary>
    /// Estudiante becado.
    /// </summary>
    /// <remarks>
    /// Registra entrada/salida con evidencia fotográfica, reporta ausencias, consulta horas acumuladas
    /// y actualiza su horario cada semestre. Solo tiene acceso a sus propios datos.
    /// </remarks>
    Beca = 3
}
