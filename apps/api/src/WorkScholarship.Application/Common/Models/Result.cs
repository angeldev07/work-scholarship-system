namespace WorkScholarship.Application.Common.Models;

/// <summary>
/// Representa el resultado de una operación que puede tener éxito o fallar.
/// Implementa el patrón Result para manejo de errores sin excepciones.
/// </summary>
public class Result
{
    /// <summary>
    /// Constructor protegido para crear un resultado.
    /// </summary>
    /// <param name="isSuccess">Indica si la operación fue exitosa.</param>
    /// <param name="error">Error asociado (solo si isSuccess es false).</param>
    /// <exception cref="InvalidOperationException">
    /// Si isSuccess es true pero error no es null, o si isSuccess es false pero error es null.
    /// </exception>
    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess && error is not null)
            throw new InvalidOperationException("A successful result cannot have an error.");
        if (!isSuccess && error is null)
            throw new InvalidOperationException("A failure result must have an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Indica si la operación fue exitosa.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Indica si la operación falló.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Error asociado a la operación fallida (null si fue exitosa).
    /// </summary>
    public Error? Error { get; }

    /// <summary>
    /// Crea un resultado exitoso sin valor de retorno.
    /// </summary>
    /// <returns>Instancia de Result exitosa.</returns>
    public static Result Success() => new(true, null);

    /// <summary>
    /// Crea un resultado fallido con un error.
    /// </summary>
    /// <param name="error">Error que describe el fallo.</param>
    /// <returns>Instancia de Result fallida.</returns>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    /// Crea un resultado fallido con código y mensaje de error.
    /// </summary>
    /// <param name="code">Código del error.</param>
    /// <param name="message">Mensaje descriptivo del error.</param>
    /// <returns>Instancia de Result fallida.</returns>
    public static Result Failure(string code, string message) => new(false, new Error(code, message));
}

/// <summary>
/// Representa el resultado de una operación que retorna un valor en caso de éxito.
/// </summary>
/// <typeparam name="T">Tipo del valor de retorno.</typeparam>
public class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value) : base(true, null)
    {
        _value = value;
    }

    private Result(Error error) : base(false, error)
    {
        _value = default;
    }

    /// <summary>
    /// Obtiene el valor del resultado exitoso.
    /// </summary>
    /// <exception cref="InvalidOperationException">Si se intenta acceder al valor de un resultado fallido.</exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value of a failed result.");

    /// <summary>
    /// Crea un resultado exitoso con un valor.
    /// </summary>
    /// <param name="value">Valor de retorno de la operación exitosa.</param>
    /// <returns>Instancia de Result&lt;T&gt; exitosa.</returns>
    public static Result<T> Success(T value) => new(value);

    /// <summary>
    /// Crea un resultado fallido con un error.
    /// </summary>
    /// <param name="error">Error que describe el fallo.</param>
    /// <returns>Instancia de Result&lt;T&gt; fallida.</returns>
    public new static Result<T> Failure(Error error) => new(error);

    /// <summary>
    /// Crea un resultado fallido con código y mensaje de error.
    /// </summary>
    /// <param name="code">Código del error.</param>
    /// <param name="message">Mensaje descriptivo del error.</param>
    /// <returns>Instancia de Result&lt;T&gt; fallida.</returns>
    public new static Result<T> Failure(string code, string message) => new(new Error(code, message));
}

/// <summary>
/// Representa un error con código, mensaje y detalles de validación opcionales.
/// </summary>
/// <param name="Code">Código único del error (ej: INVALID_CREDENTIALS, VALIDATION_ERROR).</param>
/// <param name="Message">Mensaje descriptivo del error en español para el usuario.</param>
/// <param name="Details">Lista opcional de errores de validación de campos individuales.</param>
public record Error(string Code, string Message, List<ValidationError>? Details = null);

/// <summary>
/// Representa un error de validación específico de un campo.
/// </summary>
/// <param name="Field">Nombre del campo que falló la validación.</param>
/// <param name="Message">Mensaje descriptivo del error de validación.</param>
public record ValidationError(string Field, string Message);
