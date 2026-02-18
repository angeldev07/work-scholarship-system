namespace WorkScholarship.Application.Common.Models;

/// <summary>
/// Representa la respuesta estándar de la API para todas las operaciones.
/// Proporciona formato consistente con success, message y error.
/// </summary>
public class ApiResponse
{
    /// <summary>
    /// Indica si la operación fue exitosa.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Mensaje opcional de éxito o información adicional.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Detalles del error si la operación falló (null si Success es true).
    /// </summary>
    public ApiErrorResponse? Error { get; init; }

    /// <summary>
    /// Crea una respuesta exitosa sin datos de retorno.
    /// </summary>
    /// <param name="message">Mensaje opcional de éxito.</param>
    /// <returns>Instancia de ApiResponse con Success=true.</returns>
    public static ApiResponse Ok(string? message = null) => new()
    {
        Success = true,
        Message = message
    };

    /// <summary>
    /// Crea una respuesta de error con código, mensaje y detalles opcionales de validación.
    /// </summary>
    /// <param name="code">Código del error (ej: VALIDATION_ERROR, INVALID_CREDENTIALS).</param>
    /// <param name="message">Mensaje descriptivo del error.</param>
    /// <param name="details">Lista opcional de errores de validación por campo.</param>
    /// <returns>Instancia de ApiResponse con Success=false y Error poblado.</returns>
    public static ApiResponse Fail(string code, string message, List<ValidationError>? details = null) => new()
    {
        Success = false,
        Error = new ApiErrorResponse
        {
            Code = code,
            Message = message,
            Details = details ?? []
        }
    };

    /// <summary>
    /// Convierte un Result de Application layer a ApiResponse para el controlador.
    /// </summary>
    /// <param name="result">Resultado de la operación desde el handler.</param>
    /// <param name="successMessage">Mensaje opcional a incluir si la operación fue exitosa.</param>
    /// <returns>ApiResponse con Success y Error mapeados desde Result.</returns>
    public static ApiResponse FromResult(Result result, string? successMessage = null)
    {
        if (result.IsSuccess)
            return Ok(successMessage);

        return Fail(
            result.Error!.Code,
            result.Error.Message,
            result.Error.Details);
    }
}

/// <summary>
/// Representa la respuesta estándar de la API con datos de retorno en caso de éxito.
/// </summary>
/// <typeparam name="T">Tipo de los datos de retorno.</typeparam>
public class ApiResponse<T> : ApiResponse
{
    /// <summary>
    /// Datos de retorno de la operación (null si falló).
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Crea una respuesta exitosa con datos de retorno.
    /// </summary>
    /// <param name="data">Datos a retornar en la respuesta.</param>
    /// <param name="message">Mensaje opcional de éxito.</param>
    /// <returns>Instancia de ApiResponse&lt;T&gt; con Success=true y Data poblado.</returns>
    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true,
        Data = data,
        Message = message
    };

    /// <summary>
    /// Crea una respuesta de error con código, mensaje y detalles opcionales de validación.
    /// </summary>
    /// <param name="code">Código del error.</param>
    /// <param name="message">Mensaje descriptivo del error.</param>
    /// <param name="details">Lista opcional de errores de validación por campo.</param>
    /// <returns>Instancia de ApiResponse&lt;T&gt; con Success=false y Error poblado.</returns>
    public new static ApiResponse<T> Fail(string code, string message, List<ValidationError>? details = null) => new()
    {
        Success = false,
        Error = new ApiErrorResponse
        {
            Code = code,
            Message = message,
            Details = details ?? []
        }
    };

    /// <summary>
    /// Convierte un Result&lt;T&gt; de Application layer a ApiResponse&lt;T&gt; para el controlador.
    /// </summary>
    /// <param name="result">Resultado de la operación con valor de retorno desde el handler.</param>
    /// <param name="successMessage">Mensaje opcional a incluir si la operación fue exitosa.</param>
    /// <returns>ApiResponse&lt;T&gt; con Data poblado si fue exitoso, o Error si falló.</returns>
    public static ApiResponse<T> FromResult(Result<T> result, string? successMessage = null)
    {
        if (result.IsSuccess)
            return Ok(result.Value, successMessage);

        return Fail(
            result.Error!.Code,
            result.Error.Message,
            result.Error.Details);
    }
}

/// <summary>
/// Representa los detalles de un error en la respuesta de la API.
/// </summary>
public class ApiErrorResponse
{
    /// <summary>
    /// Código único del error (ej: INVALID_CREDENTIALS, VALIDATION_ERROR).
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Mensaje descriptivo del error para mostrar al usuario.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Lista de errores de validación específicos por campo (vacía si no aplica).
    /// </summary>
    public List<ValidationError> Details { get; init; } = [];
}
