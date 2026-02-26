using System.Text.RegularExpressions;

namespace WorkScholarship.Domain.Common;

/// <summary>
/// Resultado tipado de una operación de dominio que puede tener éxito o fallar.
/// El código de error es un enum específico de cada entidad (ej: CycleErrorCode),
/// con traducción automática a string en formato UPPER_SNAKE_CASE para consumo externo.
/// </summary>
/// <typeparam name="TErrorCode">
/// Enum que define los códigos de error posibles para la operación.
/// Cada entidad de dominio define su propio enum (ej: CycleErrorCode, LocationErrorCode).
/// </typeparam>
public partial class DomainResult<TErrorCode> where TErrorCode : struct, Enum
{
    private DomainResult(bool isSuccess, TErrorCode? errorCode, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Indica si la operación de dominio fue exitosa.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Indica si la operación de dominio falló.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Código de error tipado como enum. Nulo si la operación fue exitosa.
    /// Uso interno: comparaciones compile-safe en handlers (switch/pattern matching).
    /// </summary>
    public TErrorCode? ErrorCode { get; }

    /// <summary>
    /// Código de error como string en formato UPPER_SNAKE_CASE.
    /// Traducido automáticamente desde el enum (ej: InvalidTransition → "INVALID_TRANSITION").
    /// Nulo si la operación fue exitosa.
    /// Uso externo: serialización en respuestas API para el frontend.
    /// </summary>
    public string? ErrorCodeString => ErrorCode.HasValue
        ? ToUpperSnakeCase(ErrorCode.Value.ToString())
        : null;

    /// <summary>
    /// Mensaje de error descriptivo para el usuario.
    /// Nulo si la operación fue exitosa.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Crea un resultado exitoso.
    /// </summary>
    public static DomainResult<TErrorCode> Success() => new(true, default, null);

    /// <summary>
    /// Crea un resultado fallido con código de error tipado y mensaje descriptivo.
    /// </summary>
    /// <param name="errorCode">Código de error del enum específico de la entidad.</param>
    /// <param name="message">Mensaje descriptivo del error.</param>
    public static DomainResult<TErrorCode> Failure(TErrorCode errorCode, string message) =>
        new(false, errorCode, message);

    /// <summary>
    /// Convierte PascalCase a UPPER_SNAKE_CASE.
    /// Ej: "InvalidTransition" → "INVALID_TRANSITION", "NoLocations" → "NO_LOCATIONS".
    /// </summary>
    private static string ToUpperSnakeCase(string pascalCase)
    {
        var result = UpperSnakeCaseRegex().Replace(pascalCase, "$1_$2");
        return result.ToUpperInvariant();
    }

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex UpperSnakeCaseRegex();
}
