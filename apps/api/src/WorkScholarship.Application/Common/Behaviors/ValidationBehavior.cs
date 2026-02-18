using FluentValidation;
using MediatR;
using WorkScholarship.Application.Common.Models;

namespace WorkScholarship.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior de MediatR que ejecuta validaciones de FluentValidation
/// antes de que el handler procese la solicitud.
/// </summary>
/// <typeparam name="TRequest">Tipo del request (Command o Query).</typeparam>
/// <typeparam name="TResponse">Tipo de la respuesta esperada.</typeparam>
/// <remarks>
/// Si hay errores de validación, retorna Result.Failure con código VALIDATION_ERROR
/// sin ejecutar el handler. Si no hay errores, continúa al siguiente behavior/handler.
/// </remarks>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>
    /// Inicializa el behavior con los validadores registrados en DI para TRequest.
    /// </summary>
    /// <param name="validators">Colección de validadores de FluentValidation para el request.</param>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <summary>
    /// Ejecuta las validaciones de FluentValidation antes de continuar al handler.
    /// </summary>
    /// <param name="request">Request a validar (Command o Query).</param>
    /// <param name="next">Delegado para continuar al siguiente behavior/handler.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>
    /// Result.Failure con VALIDATION_ERROR si hay errores de validación;
    /// de lo contrario, el resultado del handler.
    /// </returns>
    /// <exception cref="FluentValidation.ValidationException">
    /// Si TResponse no es Result o Result&lt;T&gt; y hay errores de validación.
    /// </exception>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next(cancellationToken);

        var validationErrors = failures
            .Select(f => new ValidationError(f.PropertyName, f.ErrorMessage))
            .ToList();

        var error = new Error(
            AuthErrorCodes.VALIDATION_ERROR,
            "Uno o mas errores de validacion ocurrieron.",
            validationErrors);

        // If the response type is a Result<T>, return a failure result
        var responseType = typeof(TResponse);

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failureMethod = responseType
                .GetMethod(nameof(Result<object>.Failure), [typeof(Error)]);

            if (failureMethod is not null)
            {
                return (TResponse)failureMethod.Invoke(null, [error])!;
            }
        }

        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        throw new FluentValidation.ValidationException(failures);
    }
}
