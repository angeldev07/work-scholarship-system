using MediatR;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Admin.DTOs;

namespace WorkScholarship.Application.Features.Admin.Queries.GetDashboardState;

/// <summary>
/// Query para obtener el estado completo del panel de administración de un departamento.
/// </summary>
/// <param name="Department">Nombre del departamento a consultar.</param>
/// <remarks>
/// Consolida múltiples consultas de estado en una sola operación para el frontend:
/// - Conteo de ubicaciones y supervisores disponibles.
/// - Estado del ciclo actual (activo, en configuración, último cerrado).
/// - Lista de acciones pendientes para guiar al administrador.
/// </remarks>
public record GetAdminDashboardStateQuery(string Department) : IRequest<Result<AdminDashboardStateDto>>;
