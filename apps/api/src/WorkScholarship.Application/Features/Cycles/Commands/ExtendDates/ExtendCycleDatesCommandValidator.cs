using FluentValidation;

namespace WorkScholarship.Application.Features.Cycles.Commands.ExtendDates;

/// <summary>
/// Validador de FluentValidation para ExtendCycleDatesCommand.
/// </summary>
/// <remarks>
/// Verifica las precondiciones básicas antes de que el handler procese el comando.
/// Las reglas de negocio de dominio (coherencia de fechas, estado del ciclo) se verifican en el handler.
/// </remarks>
public class ExtendCycleDatesCommandValidator : AbstractValidator<ExtendCycleDatesCommand>
{
    /// <summary>
    /// Define las reglas de validación para el comando de extensión de fechas.
    /// </summary>
    public ExtendCycleDatesCommandValidator()
    {
        RuleFor(x => x.CycleId)
            .NotEmpty().WithMessage("El identificador del ciclo es requerido.");

        RuleFor(x => x)
            .Must(x => x.NewApplicationDeadline.HasValue
                    || x.NewInterviewDate.HasValue
                    || x.NewSelectionDate.HasValue
                    || x.NewEndDate.HasValue)
            .WithMessage("Debe proporcionar al menos una fecha para extender.");
    }
}
