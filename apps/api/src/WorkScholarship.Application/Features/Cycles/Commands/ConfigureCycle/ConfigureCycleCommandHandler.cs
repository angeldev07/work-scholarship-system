using MediatR;
using Microsoft.EntityFrameworkCore;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Cycles.DTOs;
using WorkScholarship.Domain.Entities;
using WorkScholarship.Domain.Enums;

namespace WorkScholarship.Application.Features.Cycles.Commands.ConfigureCycle;

/// <summary>
/// Handler que procesa el comando de configuración de un ciclo semestral.
/// </summary>
/// <remarks>
/// Solo puede ejecutarse cuando el ciclo está en estado Configuration.
/// Realiza un "replace all" de ubicaciones, slots de horario y asignaciones de supervisores.
/// Al finalizar, recalcula el TotalScholarshipsAvailable del ciclo.
/// </remarks>
public class ConfigureCycleCommandHandler : IRequestHandler<ConfigureCycleCommand, Result<CycleDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// Inicializa el handler con las dependencias necesarias.
    /// </summary>
    /// <param name="context">Contexto de base de datos de la aplicación.</param>
    /// <param name="currentUserService">Servicio para obtener el usuario autenticado actual.</param>
    public ConfigureCycleCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Procesa el comando de configuración del ciclo.
    /// </summary>
    /// <param name="request">Comando con las ubicaciones, horarios y supervisores a configurar.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>
    /// Result con CycleDto actualizado en caso de éxito;
    /// Result.Failure con código de error en caso de fallo.
    /// </returns>
    /// <remarks>
    /// Códigos de error posibles:
    /// - CYCLE_NOT_FOUND: El ciclo con el Id indicado no existe.
    /// - NOT_IN_CONFIGURATION: El ciclo no está en estado Configuration.
    /// </remarks>
    public async Task<Result<CycleDto>> Handle(ConfigureCycleCommand request, CancellationToken cancellationToken)
    {
        var cycle = await _context.Cycles
            .FirstOrDefaultAsync(c => c.Id == request.CycleId, cancellationToken);

        if (cycle is null)
        {
            return Result<CycleDto>.Failure(
                $"{CycleAppError.CYCLE_NOT_FOUND}",
                "El ciclo especificado no fue encontrado.");
        }

        if (cycle.Status != CycleStatus.Configuration)
        {
            return Result<CycleDto>.Failure(
                $"{CycleAppError.NOT_IN_CONFIGURATION}",
                "Solo se puede configurar un ciclo en estado Configuration.");
        }

        var updatedBy = _currentUserService.Email ?? _currentUserService.UserId?.ToString() ?? "system";

        await ProcessLocationsAsync(cycle, request.Locations, updatedBy, cancellationToken);
        await ProcessSupervisorAssignmentsAsync(cycle, request.SupervisorAssignments, updatedBy, cancellationToken);
        await RecalculateTotalScholarshipsAsync(cycle, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var locationsCount = await _context.CycleLocations
            .CountAsync(cl => cl.CycleId == cycle.Id && cl.IsActive, cancellationToken);

        var supervisorsCount = await _context.SupervisorAssignments
            .CountAsync(sa => sa.CycleId == cycle.Id, cancellationToken);

        return Result<CycleDto>.Success(CycleDto.FromEntity(cycle, locationsCount, supervisorsCount));
    }

    /// <summary>
    /// Procesa las ubicaciones del request: crea nuevas CycleLocations, actualiza las existentes,
    /// desactiva las que ya no aparecen en el request y reemplaza sus ScheduleSlots.
    /// </summary>
    private async Task ProcessLocationsAsync(
        Cycle cycle,
        List<CycleLocationInput> locationInputs,
        string updatedBy,
        CancellationToken cancellationToken)
    {
        var existingCycleLocations = await _context.CycleLocations
            .Include(cl => cl.ScheduleSlots)
            .Where(cl => cl.CycleId == cycle.Id)
            .ToListAsync(cancellationToken);

        var requestedLocationIds = locationInputs.Select(l => l.LocationId).ToHashSet();

        // Desactivar CycleLocations que no están en el request
        foreach (var existing in existingCycleLocations.Where(cl => !requestedLocationIds.Contains(cl.LocationId)))
        {
            existing.Deactivate(updatedBy);
        }

        // Crear o actualizar CycleLocations del request
        foreach (var locationInput in locationInputs)
        {
            var existingCycleLocation = existingCycleLocations
                .FirstOrDefault(cl => cl.LocationId == locationInput.LocationId);

            if (existingCycleLocation is null)
            {
                // Nueva CycleLocation
                var newCycleLocation = CycleLocation.Create(
                    cycleId: cycle.Id,
                    locationId: locationInput.LocationId,
                    scholarshipsAvailable: locationInput.ScholarshipsAvailable,
                    createdBy: updatedBy);

                _context.CycleLocations.Add(newCycleLocation);

                foreach (var slotInput in locationInput.ScheduleSlots)
                {
                    var slot = ScheduleSlot.Create(
                        cycleLocationId: newCycleLocation.Id,
                        dayOfWeek: slotInput.DayOfWeek,
                        startTime: slotInput.StartTime,
                        endTime: slotInput.EndTime,
                        requiredScholars: slotInput.RequiredScholars,
                        createdBy: updatedBy);

                    _context.ScheduleSlots.Add(slot);
                }
            }
            else
            {
                // Actualizar CycleLocation existente
                existingCycleLocation.UpdateScholarshipsAvailable(locationInput.ScholarshipsAvailable, updatedBy);

                if (locationInput.IsActive)
                    existingCycleLocation.Activate(updatedBy);
                else
                    existingCycleLocation.Deactivate(updatedBy);

                // Reemplazar ScheduleSlots: eliminar existentes y crear nuevos
                _context.ScheduleSlots.RemoveRange(existingCycleLocation.ScheduleSlots);

                foreach (var slotInput in locationInput.ScheduleSlots)
                {
                    var slot = ScheduleSlot.Create(
                        cycleLocationId: existingCycleLocation.Id,
                        dayOfWeek: slotInput.DayOfWeek,
                        startTime: slotInput.StartTime,
                        endTime: slotInput.EndTime,
                        requiredScholars: slotInput.RequiredScholars,
                        createdBy: updatedBy);

                    _context.ScheduleSlots.Add(slot);
                }
            }
        }
    }

    /// <summary>
    /// Reemplaza todas las asignaciones de supervisores del ciclo con las provistas en el request.
    /// </summary>
    private async Task ProcessSupervisorAssignmentsAsync(
        Cycle cycle,
        List<SupervisorAssignmentInput> assignmentInputs,
        string createdBy,
        CancellationToken cancellationToken)
    {
        var existingAssignments = await _context.SupervisorAssignments
            .Where(sa => sa.CycleId == cycle.Id)
            .ToListAsync(cancellationToken);

        _context.SupervisorAssignments.RemoveRange(existingAssignments);

        foreach (var assignmentInput in assignmentInputs)
        {
            var newAssignment = SupervisorAssignment.Create(
                cycleId: cycle.Id,
                cycleLocationId: assignmentInput.CycleLocationId,
                supervisorId: assignmentInput.SupervisorId,
                createdBy: createdBy);

            _context.SupervisorAssignments.Add(newAssignment);
        }
    }

    /// <summary>
    /// Recalcula el TotalScholarshipsAvailable del ciclo sumando las becas de las CycleLocations activas.
    /// Usa EF Core change tracking para actualizar la propiedad con private setter sin invocar ExecuteUpdateAsync,
    /// garantizando compatibilidad con el proveedor InMemory usado en pruebas unitarias.
    /// </summary>
    private async Task RecalculateTotalScholarshipsAsync(Cycle cycle, CancellationToken cancellationToken)
    {
        var totalScholarships = await _context.CycleLocations
            .Where(cl => cl.CycleId == cycle.Id && cl.IsActive)
            .SumAsync(cl => cl.ScholarshipsAvailable, cancellationToken);

        // Actualizar la propiedad private set vía EF Core change tracking.
        // ApplicationDbContext hereda de DbContext, exponiendo el método Entry().
        // Esto es compatible con todos los proveedores (InMemory, PostgreSQL).
        if (_context is Microsoft.EntityFrameworkCore.DbContext dbContext)
        {
            dbContext.Entry(cycle).Property(nameof(Cycle.TotalScholarshipsAvailable)).CurrentValue = totalScholarships;
        }
    }
}
