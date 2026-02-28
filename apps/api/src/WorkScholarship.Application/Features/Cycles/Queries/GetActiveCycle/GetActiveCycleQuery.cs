using MediatR;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Cycles.DTOs;

namespace WorkScholarship.Application.Features.Cycles.Queries.GetActiveCycle;

/// <summary>
/// Query para obtener el ciclo activo de un departamento, si existe.
/// </summary>
/// <param name="Department">Nombre del departamento a consultar.</param>
/// <remarks>
/// Un ciclo "activo" en el contexto de esta query es cualquier ciclo que NO esté en estado Closed.
/// Puede estar en Configuration, ApplicationsOpen, ApplicationsClosed o Active.
/// Retorna null si no hay ningún ciclo abierto para el departamento.
/// </remarks>
public record GetActiveCycleQuery(string Department) : IRequest<Result<CycleDto?>>;
