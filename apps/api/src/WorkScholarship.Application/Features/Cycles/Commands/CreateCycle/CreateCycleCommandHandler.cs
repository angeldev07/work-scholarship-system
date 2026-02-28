using MediatR;
using Microsoft.EntityFrameworkCore;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Cycles.DTOs;
using WorkScholarship.Domain.Entities;
using WorkScholarship.Domain.Enums;

namespace WorkScholarship.Application.Features.Cycles.Commands.CreateCycle;

/// <summary>
/// Handler que procesa el comando de creación de un ciclo semestral del programa de becas trabajo.
/// </summary>
/// <remarks>
/// Flujo de ejecución:
/// 1. Verifica RN-001: no debe existir un ciclo no cerrado para el departamento.
/// 2. Determina si es el primer ciclo del departamento (para auto-completar renovaciones).
/// 3. Crea el ciclo con el factory method Cycle.Create().
/// 4. Si se proveyó CloneFromCycleId, valida que la fuente esté Closed y clona ubicaciones y horarios.
/// 5. Persiste el ciclo y retorna el CycleDto.
/// </remarks>
public class CreateCycleCommandHandler : IRequestHandler<CreateCycleCommand, Result<CycleDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// Inicializa el handler con las dependencias necesarias.
    /// </summary>
    /// <param name="context">Contexto de base de datos de la aplicación.</param>
    /// <param name="currentUserService">Servicio para obtener el usuario autenticado actual.</param>
    public CreateCycleCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Procesa el comando de creación de ciclo aplicando las reglas de negocio.
    /// </summary>
    /// <param name="request">Comando con los datos del nuevo ciclo.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>
    /// Result con CycleDto del ciclo creado en caso de éxito;
    /// Result.Failure con código de error en caso de violación de reglas de negocio.
    /// </returns>
    /// <remarks>
    /// Códigos de error posibles:
    /// - DUPLICATE_CYCLE: Ya existe un ciclo no cerrado para el departamento (RN-001).
    /// - CYCLE_NOT_FOUND: El CloneFromCycleId proporcionado no existe.
    /// - INVALID_CLONE_SOURCE: El ciclo fuente no está en estado Closed.
    /// </remarks>
    public async Task<Result<CycleDto>> Handle(CreateCycleCommand request, CancellationToken cancellationToken)
    {
        var department = request.Department.Trim();

        // RN-001: Solo puede existir un ciclo no cerrado por departamento
        var hasOpenCycle = await _context.Cycles
            .AnyAsync(c => c.Department == department && c.Status != CycleStatus.Closed, cancellationToken);

        if (hasOpenCycle)
        {
            return Result<CycleDto>.Failure(
                $"{CycleAppError.DUPLICATE_CYCLE}",
                $"Ya existe un ciclo activo o en configuración para la dependencia '{department}'. Ciérrelo antes de crear uno nuevo.");
        }

        // Si es el primer ciclo del departamento, no hay becas previas para renovar
        var isFirstCycle = !await _context.Cycles
            .AnyAsync(c => c.Department == department, cancellationToken);

        var createdBy = _currentUserService.Email ?? _currentUserService.UserId?.ToString() ?? "system";

        // Crear el ciclo mediante el factory method de dominio (puede lanzar ArgumentException)
        var cycle = Cycle.Create(
            name: request.Name,
            department: department,
            startDate: request.StartDate,
            endDate: request.EndDate,
            applicationDeadline: request.ApplicationDeadline,
            interviewDate: request.InterviewDate,
            selectionDate: request.SelectionDate,
            totalScholarshipsAvailable: request.TotalScholarshipsAvailable,
            createdBy: createdBy);

        if (isFirstCycle)
        {
            cycle.MarkRenewalProcessCompleted();
        }

        // Procesar clonación si se indicó un ciclo fuente
        if (request.CloneFromCycleId.HasValue)
        {
            var cloneResult = await CloneConfigurationAsync(cycle, request.CloneFromCycleId.Value, createdBy, cancellationToken);
            if (cloneResult is not null)
            {
                return cloneResult;
            }
        }
        else
        {
            _context.Cycles.Add(cycle);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var locationsCount = await _context.CycleLocations
            .CountAsync(cl => cl.CycleId == cycle.Id && cl.IsActive, cancellationToken);

        var supervisorsCount = await _context.SupervisorAssignments
            .CountAsync(sa => sa.CycleId == cycle.Id, cancellationToken);

        return Result<CycleDto>.Success(CycleDto.FromEntity(cycle, locationsCount, supervisorsCount));
    }

    /// <summary>
    /// Clona la configuración (ubicaciones y horarios) de un ciclo fuente al ciclo nuevo.
    /// Los supervisores no se clonan; el administrador los asignará vía ConfigureCycleCommand.
    /// </summary>
    /// <param name="newCycle">El ciclo recién creado al que se copiará la configuración.</param>
    /// <param name="sourceCycleId">Id del ciclo Closed del que se clona la configuración.</param>
    /// <param name="createdBy">Identificador del usuario que realiza la operación.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Null si la clonación fue exitosa; Result con error en caso contrario.</returns>
    private async Task<Result<CycleDto>?> CloneConfigurationAsync(
        Cycle newCycle,
        Guid sourceCycleId,
        string createdBy,
        CancellationToken cancellationToken)
    {
        var sourceClycle = await _context.Cycles
            .Include(c => c.CycleLocations)
                .ThenInclude(cl => cl.ScheduleSlots)
            .FirstOrDefaultAsync(c => c.Id == sourceCycleId, cancellationToken);

        if (sourceClycle is null)
        {
            return Result<CycleDto>.Failure(
                $"{CycleAppError.CYCLE_NOT_FOUND}",
                "El ciclo fuente para clonación no fue encontrado.");
        }

        if (!sourceClycle.IsClosed)
        {
            return Result<CycleDto>.Failure(
                $"{CycleAppError.INVALID_CLONE_SOURCE}",
                "El ciclo fuente para clonación debe estar en estado Closed.");
        }

        newCycle.SetClonedFromCycleId(sourceClycle.Id);
        _context.Cycles.Add(newCycle);

        foreach (var sourceCycleLocation in sourceClycle.CycleLocations)
        {
            var newCycleLocation = CycleLocation.Create(
                cycleId: newCycle.Id,
                locationId: sourceCycleLocation.LocationId,
                scholarshipsAvailable: sourceCycleLocation.ScholarshipsAvailable,
                createdBy: createdBy);

            _context.CycleLocations.Add(newCycleLocation);

            foreach (var sourceSlot in sourceCycleLocation.ScheduleSlots)
            {
                var newSlot = ScheduleSlot.Create(
                    cycleLocationId: newCycleLocation.Id,
                    dayOfWeek: sourceSlot.DayOfWeek,
                    startTime: sourceSlot.StartTime,
                    endTime: sourceSlot.EndTime,
                    requiredScholars: sourceSlot.RequiredScholars,
                    createdBy: createdBy);

                _context.ScheduleSlots.Add(newSlot);
            }
        }

        return null;
    }
}
