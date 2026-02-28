using MediatR;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Cycles.DTOs;

namespace WorkScholarship.Application.Features.Cycles.Commands.ReopenApplications;

/// <summary>
/// Comando para reabrir el período de postulaciones de un ciclo semestral.
/// Transiciona el ciclo del estado ApplicationsClosed de vuelta al estado ApplicationsOpen.
/// </summary>
/// <remarks>
/// Este comando actúa como "válvula de escape" cuando el administrador necesita
/// ampliar el plazo de postulaciones tras haberlas cerrado previamente.
/// Precondición validada en el dominio:
/// - El ciclo debe estar en estado ApplicationsClosed.
/// </remarks>
/// <param name="CycleId">Identificador único del ciclo a reabrir postulaciones.</param>
public record ReopenApplicationsCommand(Guid CycleId) : IRequest<Result<CycleDto>>;
