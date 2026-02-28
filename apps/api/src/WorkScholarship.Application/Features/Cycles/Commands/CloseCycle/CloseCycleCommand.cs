using MediatR;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Cycles.DTOs;

namespace WorkScholarship.Application.Features.Cycles.Commands.CloseCycle;

/// <summary>
/// Comando para cerrar oficialmente un ciclo semestral.
/// Transiciona el ciclo del estado Active al estado Closed.
/// </summary>
/// <remarks>
/// Precondiciones validadas en el dominio:
/// - El ciclo debe estar en estado Active.
/// - La fecha actual debe ser posterior a EndDate del ciclo.
/// - No deben existir jornadas pendientes de aprobación (pendingShiftsCount = 0).
/// - No deben faltar bitácoras por generar (missingLogbooksCount = 0).
/// Una vez cerrado, el ciclo es un snapshot histórico inmutable.
/// Los subsistemas TRACK (RF-029-034) y DOC (RF-040-042) no están implementados aún,
/// por lo que los conteos se pasan como 0.
/// </remarks>
/// <param name="CycleId">Identificador único del ciclo a cerrar.</param>
public record CloseCycleCommand(Guid CycleId) : IRequest<Result<CycleDto>>;
