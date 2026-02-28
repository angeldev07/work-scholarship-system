using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Admin.DTOs;
using WorkScholarship.Application.Features.Admin.Queries.GetDashboardState;
using WorkScholarship.Application.Features.Cycles.Commands.ConfigureCycle;
using WorkScholarship.Application.Features.Cycles.Commands.CreateCycle;
using WorkScholarship.Application.Features.Cycles.DTOs;
using WorkScholarship.Application.Features.Cycles.Queries.GetActiveCycle;
using WorkScholarship.Application.Features.Cycles.Queries.GetCycleById;
using WorkScholarship.Application.Features.Cycles.Queries.ListCycles;
using WorkScholarship.Domain.Enums;

namespace WorkScholarship.WebAPI.Controllers;

/// <summary>
/// Controlador REST para operaciones de ciclos semestrales del programa de becas trabajo.
/// </summary>
/// <remarks>
/// Todos los endpoints requieren autenticación con rol Admin.
/// Endpoints implementados:
/// - POST /api/cycles: Crear nuevo ciclo semestral
/// - GET /api/cycles: Listar ciclos con filtros y paginación
/// - GET /api/cycles/active: Obtener ciclo activo por departamento
/// - GET /api/cycles/{id}: Obtener detalle completo de un ciclo
/// - PUT /api/cycles/{id}/configure: Configurar ubicaciones, supervisores y horarios del ciclo
/// </remarks>
[ApiController]
[Route("api/cycles")]
[Authorize(Roles = "Admin")]
public class CyclesController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa el controlador con el sender de MediatR.
    /// </summary>
    /// <param name="sender">Sender de MediatR para enviar Commands/Queries.</param>
    public CyclesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Crear un nuevo ciclo semestral para un departamento.
    /// </summary>
    /// <param name="command">Datos del nuevo ciclo incluyendo fechas, departamento y becas disponibles.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>
    /// 201 Created: ApiResponse con CycleDto del ciclo creado.
    /// 400 Bad Request: Errores de validación (VALIDATION_ERROR) o duplicado (DUPLICATE_CYCLE).
    /// 401 Unauthorized: No autenticado.
    /// 403 Forbidden: No tiene rol Admin.
    /// </returns>
    /// <remarks>
    /// Regla de negocio RN-001: Solo puede existir un ciclo no cerrado por departamento.
    /// Si se provee CloneFromCycleId, el ciclo fuente debe estar en estado Closed.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CycleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCycleCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(ApiResponse<CycleDto>.Fail(
                result.Error!.Code,
                result.Error.Message,
                result.Error.Details));
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value.Id },
            ApiResponse<CycleDto>.Ok(result.Value, "Ciclo creado exitosamente."));
    }

    /// <summary>
    /// Obtener lista paginada de ciclos con filtros opcionales.
    /// </summary>
    /// <param name="department">Filtro por nombre de departamento (opcional).</param>
    /// <param name="year">Filtro por año del ciclo basado en StartDate (opcional).</param>
    /// <param name="status">Filtro por estado del ciclo (opcional).</param>
    /// <param name="page">Número de página (base 1). Por defecto: 1.</param>
    /// <param name="pageSize">Cantidad de elementos por página. Por defecto: 10.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>
    /// 200 OK: ApiResponse con lista paginada de CycleListItemDto.
    /// 401 Unauthorized: No autenticado.
    /// 403 Forbidden: No tiene rol Admin.
    /// </returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedList<CycleListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? department,
        [FromQuery] int? year,
        [FromQuery] CycleStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new ListCyclesQuery
        {
            Department = department,
            Year = year,
            Status = status,
            Page = page,
            PageSize = pageSize
        };

        var result = await _sender.Send(query, cancellationToken);

        return Ok(ApiResponse<PaginatedList<CycleListItemDto>>.Ok(result.Value));
    }

    /// <summary>
    /// Obtener el ciclo activo (no cerrado) de un departamento.
    /// </summary>
    /// <param name="department">Nombre del departamento a consultar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>
    /// 200 OK: ApiResponse con CycleDto del ciclo activo, o null si no existe ninguno.
    /// 400 Bad Request: Departamento no especificado (VALIDATION_ERROR).
    /// 401 Unauthorized: No autenticado.
    /// 403 Forbidden: No tiene rol Admin.
    /// </returns>
    [HttpGet("active")]
    [ProducesResponseType(typeof(ApiResponse<CycleDto?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetActive(
        [FromQuery] string department,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(department))
        {
            return BadRequest(ApiResponse.Fail(
                "VALIDATION_ERROR",
                "El parámetro 'department' es requerido."));
        }

        var query = new GetActiveCycleQuery(department);
        var result = await _sender.Send(query, cancellationToken);

        return Ok(ApiResponse<CycleDto?>.Ok(result.Value));
    }

    /// <summary>
    /// Obtener el detalle completo de un ciclo por su identificador.
    /// </summary>
    /// <param name="id">Identificador único del ciclo.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>
    /// 200 OK: ApiResponse con CycleDetailDto del ciclo.
    /// 404 Not Found: Ciclo no encontrado (CYCLE_NOT_FOUND).
    /// 401 Unauthorized: No autenticado.
    /// 403 Forbidden: No tiene rol Admin.
    /// </returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CycleDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetCycleByIdQuery(id);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(ApiResponse.Fail(result.Error!.Code, result.Error.Message));
        }

        return Ok(ApiResponse<CycleDetailDto>.Ok(result.Value));
    }

    /// <summary>
    /// Configurar las ubicaciones, supervisores y horarios de un ciclo.
    /// Solo válido cuando el ciclo está en estado Configuration.
    /// </summary>
    /// <param name="id">Identificador del ciclo a configurar.</param>
    /// <param name="command">Datos de configuración con ubicaciones, horarios y supervisores.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>
    /// 200 OK: ApiResponse con CycleDto actualizado.
    /// 400 Bad Request: Errores de validación (VALIDATION_ERROR) o estado inválido (NOT_IN_CONFIGURATION).
    /// 404 Not Found: Ciclo no encontrado (CYCLE_NOT_FOUND).
    /// 401 Unauthorized: No autenticado.
    /// 403 Forbidden: No tiene rol Admin.
    /// </returns>
    /// <remarks>
    /// Esta operación realiza un "replace all":
    /// - Las ubicaciones no incluidas se desactivan.
    /// - Los ScheduleSlots de cada ubicación son reemplazados completamente.
    /// - Las asignaciones de supervisores son reemplazadas completamente.
    /// El TotalScholarshipsAvailable del ciclo se recalcula automáticamente.
    /// </remarks>
    [HttpPut("{id:guid}/configure")]
    [ProducesResponseType(typeof(ApiResponse<CycleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Configure(
        Guid id,
        [FromBody] ConfigureCycleCommand command,
        CancellationToken cancellationToken)
    {
        // Asegurar que el CycleId del route coincide con el body
        var commandWithId = command with { CycleId = id };
        var result = await _sender.Send(commandWithId, cancellationToken);

        if (result.IsFailure)
        {
            var statusCode = result.Error!.Code == $"{CycleAppError.CYCLE_NOT_FOUND}"
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;

            return StatusCode(statusCode, ApiResponse.Fail(
                result.Error.Code,
                result.Error.Message,
                result.Error.Details));
        }

        return Ok(ApiResponse<CycleDto>.Ok(result.Value, "Ciclo configurado exitosamente."));
    }
}

/// <summary>
/// Controlador REST para operaciones del panel de administración.
/// </summary>
/// <remarks>
/// Agrupa endpoints de estado y métricas de administración.
/// </remarks>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa el controlador con el sender de MediatR.
    /// </summary>
    /// <param name="sender">Sender de MediatR para enviar Queries.</param>
    public AdminController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Obtener el estado completo del panel de administración para un departamento.
    /// </summary>
    /// <param name="department">Nombre del departamento a consultar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>
    /// 200 OK: ApiResponse con AdminDashboardStateDto con el estado completo del departamento.
    /// 400 Bad Request: Departamento no especificado.
    /// 401 Unauthorized: No autenticado.
    /// 403 Forbidden: No tiene rol Admin.
    /// </returns>
    /// <remarks>
    /// Esta query es la "consulta de salud" del panel de administración.
    /// El frontend la usa para determinar qué vista mostrar:
    /// - Estado vacío (sin ubicaciones ni supervisores): wizard de onboarding
    /// - Ciclo en configuración: continuar wizard
    /// - Sin ciclo activo: crear nuevo ciclo o activar existente
    /// - Ciclo activo: dashboard de métricas
    /// </remarks>
    [HttpGet("dashboard-state")]
    [ProducesResponseType(typeof(ApiResponse<AdminDashboardStateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDashboardState(
        [FromQuery] string department,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(department))
        {
            return BadRequest(ApiResponse.Fail(
                "VALIDATION_ERROR",
                "El parámetro 'department' es requerido."));
        }

        var query = new GetAdminDashboardStateQuery(department);
        var result = await _sender.Send(query, cancellationToken);

        return Ok(ApiResponse<AdminDashboardStateDto>.Ok(result.Value));
    }
}
