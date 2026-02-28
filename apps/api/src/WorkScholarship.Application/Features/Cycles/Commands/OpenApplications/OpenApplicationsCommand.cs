using MediatR;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Cycles.DTOs;

namespace WorkScholarship.Application.Features.Cycles.Commands.OpenApplications;

/// <summary>
/// Comando para abrir el período de postulaciones de un ciclo semestral.
/// Transiciona el ciclo del estado Configuration al estado ApplicationsOpen.
/// </summary>
/// <remarks>
/// Precondiciones validadas en el dominio:
/// - El ciclo debe estar en estado Configuration.
/// - Debe tener al menos una ubicación activa configurada.
/// - TotalScholarshipsAvailable debe ser mayor a 0.
/// - El proceso de renovaciones debe estar completado (RenewalProcessCompleted = true).
/// </remarks>
/// <param name="CycleId">Identificador único del ciclo a abrir.</param>
public record OpenApplicationsCommand(Guid CycleId) : IRequest<Result<CycleDto>>;
