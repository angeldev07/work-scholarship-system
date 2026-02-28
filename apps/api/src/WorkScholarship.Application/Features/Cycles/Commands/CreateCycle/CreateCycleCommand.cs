using MediatR;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Cycles.DTOs;

namespace WorkScholarship.Application.Features.Cycles.Commands.CreateCycle;

/// <summary>
/// Comando para crear un nuevo ciclo semestral del programa de becas trabajo.
/// </summary>
/// <remarks>
/// Regla de negocio RN-001: Solo puede existir un ciclo no cerrado por departamento.
/// Si ya existe uno en cualquier estado distinto a Closed, el comando retorna DUPLICATE_CYCLE.
/// Si es el primer ciclo del departamento, se marca automáticamente RenewalProcessCompleted = true.
/// Si se proporciona CloneFromCycleId, el ciclo fuente debe estar en estado Closed.
/// </remarks>
public record CreateCycleCommand : IRequest<Result<CycleDto>>
{
    /// <summary>
    /// Nombre del ciclo (ej: "2024-2", "Agosto-Diciembre 2024"). Máximo 100 caracteres.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Nombre del departamento o dependencia universitaria propietaria del ciclo. Máximo 100 caracteres.
    /// </summary>
    public string Department { get; init; } = string.Empty;

    /// <summary>
    /// Fecha de inicio del período académico. Debe ser una fecha futura.
    /// </summary>
    public DateTime StartDate { get; init; }

    /// <summary>
    /// Fecha de fin del período académico. Debe ser posterior a StartDate.
    /// </summary>
    public DateTime EndDate { get; init; }

    /// <summary>
    /// Fecha límite para recibir postulaciones. Debe ser futura y anterior a InterviewDate.
    /// </summary>
    public DateTime ApplicationDeadline { get; init; }

    /// <summary>
    /// Fecha programada para la realización de entrevistas. Debe ser anterior a SelectionDate.
    /// </summary>
    public DateTime InterviewDate { get; init; }

    /// <summary>
    /// Fecha de selección final de becarios. Debe ser anterior a EndDate.
    /// </summary>
    public DateTime SelectionDate { get; init; }

    /// <summary>
    /// Total de plazas de beca disponibles en el ciclo. Debe ser mayor a 0.
    /// </summary>
    public int TotalScholarshipsAvailable { get; init; }

    /// <summary>
    /// Identificador del ciclo cerrado del que clonar la configuración (ubicaciones, supervisores, horarios).
    /// Si se proporciona, el ciclo fuente debe estar en estado Closed.
    /// Nulo si la configuración se establece manualmente.
    /// </summary>
    public Guid? CloneFromCycleId { get; init; }
}
