using MediatR;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Cycles.DTOs;

namespace WorkScholarship.Application.Features.Cycles.Queries.GetCycleById;

/// <summary>
/// Query para obtener el detalle completo de un ciclo semestral por su identificador.
/// </summary>
/// <param name="Id">Identificador único del ciclo a consultar.</param>
/// <remarks>
/// Retorna un CycleDetailDto con la información completa del ciclo incluyendo:
/// cantidad de ubicaciones activas, supervisores asignados y becarios (módulo SEL).
/// </remarks>
public record GetCycleByIdQuery(Guid Id) : IRequest<Result<CycleDetailDto>>;
