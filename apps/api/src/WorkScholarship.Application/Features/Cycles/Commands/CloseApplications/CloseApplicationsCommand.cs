using MediatR;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Cycles.DTOs;

namespace WorkScholarship.Application.Features.Cycles.Commands.CloseApplications;

/// <summary>
/// Comando para cerrar el período de postulaciones de un ciclo semestral.
/// Transiciona el ciclo del estado ApplicationsOpen al estado ApplicationsClosed.
/// </summary>
/// <remarks>
/// Precondición validada en el dominio:
/// - El ciclo debe estar en estado ApplicationsOpen.
/// Una vez cerradas, las postulaciones pueden reabrirse con ReopenApplicationsCommand
/// o el ciclo puede avanzar al proceso de selección.
/// </remarks>
/// <param name="CycleId">Identificador único del ciclo a cerrar postulaciones.</param>
public record CloseApplicationsCommand(Guid CycleId) : IRequest<Result<CycleDto>>;
