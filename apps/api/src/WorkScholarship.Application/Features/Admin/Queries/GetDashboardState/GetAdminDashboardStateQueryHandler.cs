using MediatR;
using Microsoft.EntityFrameworkCore;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Admin.DTOs;
using WorkScholarship.Application.Features.Cycles.DTOs;
using WorkScholarship.Domain.Enums;

namespace WorkScholarship.Application.Features.Admin.Queries.GetDashboardState;

/// <summary>
/// Handler que construye el estado del panel de administración de un departamento.
/// </summary>
/// <remarks>
/// Ejecuta múltiples consultas eficientes (sin N+1) para construir el snapshot completo del estado:
/// - Conteo de ubicaciones activas del departamento.
/// - Conteo de supervisores activos (global, no por departamento).
/// - Ciclo activo (estado Active) del departamento.
/// - Ciclo en configuración (Configuration, ApplicationsOpen, ApplicationsClosed) del departamento.
/// - Último ciclo cerrado del departamento.
/// - Cálculo de acciones pendientes basado en el estado.
/// </remarks>
public class GetAdminDashboardStateQueryHandler : IRequestHandler<GetAdminDashboardStateQuery, Result<AdminDashboardStateDto>>
{
    private readonly IApplicationDbContext _context;

    /// <summary>
    /// Inicializa el handler con el contexto de base de datos.
    /// </summary>
    /// <param name="context">Contexto de base de datos de la aplicación.</param>
    public GetAdminDashboardStateQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Procesa la query construyendo el estado completo del panel de administración.
    /// </summary>
    /// <param name="request">Query con el nombre del departamento.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>
    /// Result exitoso con AdminDashboardStateDto con el estado completo del departamento.
    /// Siempre es exitoso — un estado vacío es un estado válido (nuevo departamento).
    /// </returns>
    public async Task<Result<AdminDashboardStateDto>> Handle(
        GetAdminDashboardStateQuery request,
        CancellationToken cancellationToken)
    {
        var department = request.Department.Trim().ToLowerInvariant();

        // 1. Contar ubicaciones activas del departamento
        var locationsCount = await _context.Locations
            .CountAsync(l => l.Department.ToLower() == department && l.IsActive, cancellationToken);

        // 2. Contar supervisores activos del sistema (global, no filtrado por departamento)
        var supervisorsCount = await _context.Users
            .CountAsync(u => u.Role == UserRole.Supervisor && u.IsActive, cancellationToken);

        // 3. Obtener ciclos del departamento (ordenados por fecha de creación descendente)
        var cycles = await _context.Cycles
            .AsNoTracking()
            .Where(c => c.Department.ToLower() == department)
            .OrderByDescending(c => c.CreatedAt)
            .Take(10) // Limitar para optimizar la query
            .ToListAsync(cancellationToken);

        // Identificar el ciclo activo (estado Active)
        var activeCycleEntity = cycles.FirstOrDefault(c => c.Status == CycleStatus.Active);

        // Identificar el ciclo en configuración (cualquier estado no cerrado distinto a Active)
        var cycleInConfigurationEntity = cycles.FirstOrDefault(c =>
            c.Status == CycleStatus.Configuration ||
            c.Status == CycleStatus.ApplicationsOpen ||
            c.Status == CycleStatus.ApplicationsClosed);

        // Identificar el último ciclo cerrado
        var lastClosedCycleEntity = cycles.FirstOrDefault(c => c.Status == CycleStatus.Closed);

        // 4. Obtener contadores de ubicaciones y supervisores para cada ciclo relevante
        CycleDto? activeCycleDto = null;
        CycleDto? cycleInConfigurationDto = null;
        CycleDto? lastClosedCycleDto = null;

        if (activeCycleEntity is not null)
        {
            var (lc, sc) = await GetCycleCountsAsync(activeCycleEntity.Id, cancellationToken);
            activeCycleDto = CycleDto.FromEntity(activeCycleEntity, lc, sc);
        }

        if (cycleInConfigurationEntity is not null)
        {
            var (lc, sc) = await GetCycleCountsAsync(cycleInConfigurationEntity.Id, cancellationToken);
            cycleInConfigurationDto = CycleDto.FromEntity(cycleInConfigurationEntity, lc, sc);
        }

        if (lastClosedCycleEntity is not null)
        {
            var (lc, sc) = await GetCycleCountsAsync(lastClosedCycleEntity.Id, cancellationToken);
            lastClosedCycleDto = CycleDto.FromEntity(lastClosedCycleEntity, lc, sc);
        }

        // 5. Calcular acciones pendientes
        var pendingActions = BuildPendingActions(
            locationsCount,
            supervisorsCount,
            activeCycleDto,
            cycleInConfigurationDto);

        var dto = new AdminDashboardStateDto
        {
            HasLocations = locationsCount > 0,
            LocationsCount = locationsCount,
            HasSupervisors = supervisorsCount > 0,
            SupervisorsCount = supervisorsCount,
            ActiveCycle = activeCycleDto,
            LastClosedCycle = lastClosedCycleDto,
            CycleInConfiguration = cycleInConfigurationDto,
            PendingActions = pendingActions
        };

        return Result<AdminDashboardStateDto>.Success(dto);
    }

    /// <summary>
    /// Obtiene los contadores de ubicaciones activas y supervisores para un ciclo.
    /// </summary>
    private async Task<(int LocationsCount, int SupervisorsCount)> GetCycleCountsAsync(
        Guid cycleId,
        CancellationToken cancellationToken)
    {
        var locationsCount = await _context.CycleLocations
            .CountAsync(cl => cl.CycleId == cycleId && cl.IsActive, cancellationToken);

        var supervisorsCount = await _context.SupervisorAssignments
            .CountAsync(sa => sa.CycleId == cycleId, cancellationToken);

        return (locationsCount, supervisorsCount);
    }

    /// <summary>
    /// Construye la lista de acciones pendientes basándose en el estado actual del departamento.
    /// </summary>
    private static List<PendingActionItem> BuildPendingActions(
        int locationsCount,
        int supervisorsCount,
        CycleDto? activeCycle,
        CycleDto? cycleInConfiguration)
    {
        var actions = new List<PendingActionItem>();

        if (locationsCount == 0)
        {
            actions.Add(new PendingActionItem(PendingActionCode.NoLocations));
        }

        if (supervisorsCount == 0)
        {
            actions.Add(new PendingActionItem(PendingActionCode.NoSupervisors));
        }

        if (activeCycle is null && cycleInConfiguration is null)
        {
            actions.Add(new PendingActionItem(PendingActionCode.NoActiveCycle));
        }

        if (cycleInConfiguration is not null)
        {
            if (cycleInConfiguration.LocationsCount == 0)
            {
                actions.Add(new PendingActionItem(PendingActionCode.CycleNeedsLocations));
            }

            if (cycleInConfiguration.SupervisorsCount == 0)
            {
                actions.Add(new PendingActionItem(PendingActionCode.CycleNeedsSupervisors));
            }

            if (!cycleInConfiguration.RenewalProcessCompleted)
            {
                actions.Add(new PendingActionItem(PendingActionCode.RenewalsPending));
            }
        }

        return actions;
    }
}
