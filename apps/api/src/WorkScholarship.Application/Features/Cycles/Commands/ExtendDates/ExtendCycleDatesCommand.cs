using MediatR;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Cycles.DTOs;

namespace WorkScholarship.Application.Features.Cycles.Commands.ExtendDates;

/// <summary>
/// Comando para extender las fechas de un ciclo semestral.
/// Solo permite extender (nunca reducir) fechas en los estados Configuration, ApplicationsOpen y Active.
/// </summary>
/// <remarks>
/// Precondiciones validadas en el dominio:
/// - El ciclo NO debe estar en estado Closed (inmutable).
/// - El ciclo NO debe estar en estado ApplicationsClosed (fase de entrevistas).
/// - Cada nueva fecha debe ser estrictamente mayor a la fecha actual correspondiente.
/// - Las fechas resultantes deben mantener la coherencia temporal entre sí.
/// Al menos una fecha debe ser proporcionada (validado en FluentValidation).
/// </remarks>
public record ExtendCycleDatesCommand : IRequest<Result<CycleDto>>
{
    /// <summary>
    /// Identificador único del ciclo cuyas fechas se van a extender.
    /// </summary>
    public Guid CycleId { get; init; }

    /// <summary>
    /// Nueva fecha límite de postulación. Debe ser mayor a la fecha actual del ciclo.
    /// Nulo si no se desea modificar esta fecha.
    /// </summary>
    public DateTime? NewApplicationDeadline { get; init; }

    /// <summary>
    /// Nueva fecha de entrevistas. Debe ser mayor a la fecha actual del ciclo.
    /// Nulo si no se desea modificar esta fecha.
    /// </summary>
    public DateTime? NewInterviewDate { get; init; }

    /// <summary>
    /// Nueva fecha de selección. Debe ser mayor a la fecha actual del ciclo.
    /// Nulo si no se desea modificar esta fecha.
    /// </summary>
    public DateTime? NewSelectionDate { get; init; }

    /// <summary>
    /// Nueva fecha de fin del ciclo. Debe ser mayor a la fecha actual del ciclo.
    /// Nulo si no se desea modificar esta fecha.
    /// </summary>
    public DateTime? NewEndDate { get; init; }
}
