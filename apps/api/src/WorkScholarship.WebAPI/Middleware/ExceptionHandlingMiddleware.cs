using System.Net;
using System.Text.Json;
using WorkScholarship.Application.Common.Models;

namespace WorkScholarship.WebAPI.Middleware;

/// <summary>
/// Middleware para manejo global de excepciones no controladas en el pipeline HTTP.
/// </summary>
/// <remarks>
/// Captura excepciones que no fueron manejadas por controllers o handlers,
/// las registra en logs y retorna respuestas ApiResponse consistentes al cliente.
/// Tipos de excepciones manejadas:
/// - FluentValidation.ValidationException → 400 Bad Request
/// - UnauthorizedAccessException → 401 Unauthorized
/// - Exception (cualquier otra) → 500 Internal Server Error
/// </remarks>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Inicializa el middleware con el siguiente delegado del pipeline y el logger.
    /// </summary>
    /// <param name="next">Siguiente middleware/handler en el pipeline HTTP.</param>
    /// <param name="logger">Logger para registrar excepciones.</param>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Ejecuta el middleware, capturando y manejando excepciones del pipeline.
    /// </summary>
    /// <param name="context">Contexto HTTP de la petición actual.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (FluentValidation.ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation exception occurred");
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            await HandleUnauthorizedExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Maneja excepciones de validación de FluentValidation.
    /// </summary>
    /// <param name="context">Contexto HTTP.</param>
    /// <param name="exception">Excepción de validación capturada.</param>
    /// <remarks>
    /// Retorna 400 Bad Request con ApiResponse conteniendo detalles de validación por campo.
    /// Esto solo ocurre si ValidationBehavior no capturó la excepción (fallback).
    /// </remarks>
    private static async Task HandleValidationExceptionAsync(HttpContext context, FluentValidation.ValidationException exception)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        context.Response.ContentType = "application/json";

        var validationErrors = exception.Errors
            .Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))
            .ToList();

        var response = ApiResponse.Fail(
            AuthErrorCodes.VALIDATION_ERROR,
            "Uno o mas errores de validacion ocurrieron.",
            validationErrors);

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsJsonAsync(response, jsonOptions);
    }

    /// <summary>
    /// Maneja excepciones de acceso no autorizado.
    /// </summary>
    /// <param name="context">Contexto HTTP.</param>
    /// <param name="exception">Excepción de acceso no autorizado capturada.</param>
    /// <remarks>
    /// Retorna 401 Unauthorized con ApiResponse estándar.
    /// </remarks>
    private static async Task HandleUnauthorizedExceptionAsync(HttpContext context, UnauthorizedAccessException exception)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.Response.ContentType = "application/json";

        var response = ApiResponse.Fail(
            AuthErrorCodes.UNAUTHORIZED,
            "No autorizado.");

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsJsonAsync(response, jsonOptions);
    }

    /// <summary>
    /// Maneja cualquier excepción no controlada.
    /// </summary>
    /// <param name="context">Contexto HTTP.</param>
    /// <param name="exception">Excepción capturada.</param>
    /// <remarks>
    /// Retorna 500 Internal Server Error con ApiResponse genérico (sin detalles sensibles).
    /// La excepción completa se registra en logs con nivel Error para debugging.
    /// </remarks>
    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";

        var response = ApiResponse.Fail(
            "INTERNAL_SERVER_ERROR",
            "Ha ocurrido un error interno. Por favor intenta mas tarde.");

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsJsonAsync(response, jsonOptions);
    }
}
