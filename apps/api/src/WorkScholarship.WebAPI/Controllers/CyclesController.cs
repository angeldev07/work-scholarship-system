using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Admin.DTOs;
using WorkScholarship.Application.Features.Admin.Queries.GetDashboardState;
using WorkScholarship.Application.Features.Cycles.Commands.CloseApplications;
using WorkScholarship.Application.Features.Cycles.Commands.CloseCycle;
using WorkScholarship.Application.Features.Cycles.Commands.ConfigureCycle;
using WorkScholarship.Application.Features.Cycles.Commands.CreateCycle;
using WorkScholarship.Application.Features.Cycles.Commands.ExtendDates;
using WorkScholarship.Application.Features.Cycles.Commands.OpenApplications;
using WorkScholarship.Application.Features.Cycles.Commands.ReopenApplications;
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
/// - POST /api/cycles/{id}/open-applications: Abrir período de postulaciones
/// - POST /api/cycles/{id}/close-applications: Cerrar período de postulaciones
/// - POST /api/cycles/{id}/reopen-applications: Reabrir período de postulaciones
/// - POST /api/cycles/{id}/close: Cerrar oficialmente el ciclo (Active → Closed)
/// - PUT /api/cycles/{id}/extend-dates: Extender fechas del ciclo
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

    /// <summary>
    /// Abrir el período de postulaciones de un ciclo.
    /// Transiciona el ciclo de Configuration a ApplicationsOpen.
    /// </summary>
    /// <param name="id">Identificador único del ciclo.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>
    /// 200 OK: ApiResponse con CycleDto actualizado en estado ApplicationsOpen.
    /// 400 Bad Request: Precondición del dominio no cumplida (NO_LOCATIONS, NO_SCHOLARSHIPS, RENEWALS_PENDING).
    /// 404 Not Found: Ciclo no encontrado (CYCLE_NOT_FOUND).
    /// 409 Conflict: La transición de estado no es válida desde el estado actual (INVALID_TRANSITION).
    /// 401 Unauthorized: No autenticado.
    /// 403 Forbidden: No tiene rol Admin.
    /// </returns>
    [HttpPost("{id:guid}/open-applications")]
    [ProducesResponseType(typeof(ApiResponse<CycleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> OpenApplications(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new OpenApplicationsCommand(id);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return MapTransitionError(result.Error!);
        }

        return Ok(ApiResponse<CycleDto>.Ok(result.Value, "Postulaciones abiertas exitosamente."));
    }

    /// <summary>
    /// Cerrar el período de postulaciones de un ciclo.
    /// Transiciona el ciclo de ApplicationsOpen a ApplicationsClosed.
    /// </summary>
    /// <param name="id">Identificador único del ciclo.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>
    /// 200 OK: ApiResponse con CycleDto actualizado en estado ApplicationsClosed.
    /// 404 Not Found: Ciclo no encontrado (CYCLE_NOT_FOUND).
    /// 409 Conflict: La transición de estado no es válida desde el estado actual (INVALID_TRANSITION).
    /// 401 Unauthorized: No autenticado.
    /// 403 Forbidden: No tiene rol Admin.
    /// </returns>
    [HttpPost("{id:guid}/close-applications")]
    [ProducesResponseType(typeof(ApiResponse<CycleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CloseApplications(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new CloseApplicationsCommand(id);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return MapTransitionError(result.Error!);
        }

        return Ok(ApiResponse<CycleDto>.Ok(result.Value, "Postulaciones cerradas exitosamente."));
    }

    /// <summary>
    /// Reabrir el período de postulaciones de un ciclo (válvula de escape).
    /// Transiciona el ciclo de ApplicationsClosed de vuelta a ApplicationsOpen.
    /// </summary>
    /// <param name="id">Identificador único del ciclo.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>
    /// 200 OK: ApiResponse con CycleDto actualizado en estado ApplicationsOpen.
    /// 404 Not Found: Ciclo no encontrado (CYCLE_NOT_FOUND).
    /// 409 Conflict: La transición de estado no es válida desde el estado actual (INVALID_TRANSITION).
    /// 401 Unauthorized: No autenticado.
    /// 403 Forbidden: No tiene rol Admin.
    /// </returns>
    [HttpPost("{id:guid}/reopen-applications")]
    [ProducesResponseType(typeof(ApiResponse<CycleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ReopenApplications(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new ReopenApplicationsCommand(id);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return MapTransitionError(result.Error!);
        }

        return Ok(ApiResponse<CycleDto>.Ok(result.Value, "Postulaciones reabiertas exitosamente."));
    }

    /// <summary>
    /// Cerrar oficialmente el ciclo semestral.
    /// Transiciona el ciclo del estado Active al estado Closed.
    /// </summary>
    /// <param name="id">Identificador único del ciclo a cerrar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>
    /// 200 OK: ApiResponse con CycleDto actualizado en estado Closed.
    /// 400 Bad Request: Precondición del dominio no cumplida (CYCLE_NOT_ENDED).
    /// 404 Not Found: Ciclo no encontrado (CYCLE_NOT_FOUND).
    /// 409 Conflict: La transición de estado no es válida desde el estado actual (INVALID_TRANSITION).
    /// 401 Unauthorized: No autenticado.
    /// 403 Forbidden: No tiene rol Admin.
    /// </returns>
    /// <remarks>
    /// Precondiciones: ciclo en estado Active, fecha actual posterior a EndDate,
    /// 0 jornadas pendientes de aprobación y 0 becarios sin bitácora.
    /// Los subsistemas TRACK (RF-029-034) y DOC (RF-040-042) no están implementados aún;
    /// los conteos se pasan como 0 temporalmente.
    /// </remarks>
    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(typeof(ApiResponse<CycleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CloseCycle(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new CloseCycleCommand(id);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return MapTransitionError(result.Error!);
        }

        return Ok(ApiResponse<CycleDto>.Ok(result.Value, "Ciclo cerrado exitosamente."));
    }

    /// <summary>
    /// Extender las fechas de un ciclo semestral.
    /// Válido desde los estados Configuration, ApplicationsOpen y Active.
    /// </summary>
    /// <param name="id">Identificador único del ciclo a modificar.</param>
    /// <param name="command">Datos con las nuevas fechas opcionales a extender.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>
    /// 200 OK: ApiResponse con CycleDto actualizado con las nuevas fechas.
    /// 400 Bad Request: Validación fallida (VALIDATION_ERROR) o fecha inválida (INVALID_DATE, CYCLE_NOT_ENDED).
    /// 404 Not Found: Ciclo no encontrado (CYCLE_NOT_FOUND).
    /// 409 Conflict: Estado inválido para la operación (INVALID_TRANSITION, CYCLE_CLOSED).
    /// 401 Unauthorized: No autenticado.
    /// 403 Forbidden: No tiene rol Admin.
    /// </returns>
    /// <remarks>
    /// Solo se permiten extensiones (nunca reducciones) de fechas.
    /// No válido en estado ApplicationsClosed (fase de entrevistas) ni Closed (inmutable).
    /// </remarks>
    [HttpPut("{id:guid}/extend-dates")]
    [ProducesResponseType(typeof(ApiResponse<CycleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExtendDates(
        Guid id,
        [FromBody] ExtendCycleDatesCommand command,
        CancellationToken cancellationToken)
    {
        // Asegurar que el CycleId del route coincide con el body
        var commandWithId = command with { CycleId = id };
        var result = await _sender.Send(commandWithId, cancellationToken);

        if (result.IsFailure)
        {
            return MapTransitionError(result.Error!);
        }

        return Ok(ApiResponse<CycleDto>.Ok(result.Value, "Fechas del ciclo extendidas exitosamente."));
    }

    /// <summary>
    /// Mapea un error de transición de estado al código HTTP correspondiente.
    /// </summary>
    /// <param name="error">Error retornado por el handler.</param>
    /// <returns>ObjectResult con el código HTTP apropiado según el tipo de error.</returns>
    private ObjectResult MapTransitionError(Error error)
    {
        if (error.Code.Contains("NOT_FOUND"))
        {
            return StatusCode(
                StatusCodes.Status404NotFound,
                ApiResponse.Fail(error.Code, error.Message));
        }

        if (error.Code.Contains("INVALID_TRANSITION"))
        {
            return StatusCode(
                StatusCodes.Status409Conflict,
                ApiResponse.Fail(error.Code, error.Message));
        }

        if (error.Code.Contains("CYCLE_CLOSED"))
        {
            return StatusCode(
                StatusCodes.Status409Conflict,
                ApiResponse.Fail(error.Code, error.Message));
        }

        return StatusCode(
            StatusCodes.Status400BadRequest,
            ApiResponse.Fail(error.Code, error.Message));
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
