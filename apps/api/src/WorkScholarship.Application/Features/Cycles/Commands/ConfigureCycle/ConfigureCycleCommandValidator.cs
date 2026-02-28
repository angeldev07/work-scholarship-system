using FluentValidation;

namespace WorkScholarship.Application.Features.Cycles.Commands.ConfigureCycle;

/// <summary>
/// Validador de FluentValidation para ConfigureCycleCommand.
/// </summary>
/// <remarks>
/// Valida la estructura básica del comando antes de que el handler procese la lógica de negocio.
/// Las validaciones de estado del ciclo (debe estar en Configuration) se realizan en el handler.
/// </remarks>
public class ConfigureCycleCommandValidator : AbstractValidator<ConfigureCycleCommand>
{
    /// <summary>
    /// Define las reglas de validación para el comando de configuración de ciclo.
    /// </summary>
    public ConfigureCycleCommandValidator()
    {
        RuleFor(x => x.CycleId)
            .NotEmpty().WithMessage("El identificador del ciclo es requerido.");

        RuleForEach(x => x.Locations).ChildRules(location =>
        {
            location.RuleFor(l => l.LocationId)
                .NotEmpty().WithMessage("El identificador de la ubicación es requerido.");

            location.RuleFor(l => l.ScholarshipsAvailable)
                .GreaterThan(0).WithMessage("El número de becas disponibles debe ser mayor a 0.");

            location.RuleForEach(l => l.ScheduleSlots).ChildRules(slot =>
            {
                slot.RuleFor(s => s.DayOfWeek)
                    .InclusiveBetween(1, 7).WithMessage("El día de la semana debe estar entre 1 (Lunes) y 7 (Domingo).");

                slot.RuleFor(s => s.StartTime)
                    .LessThan(s => s.EndTime).WithMessage("La hora de inicio debe ser anterior a la hora de fin.");

                slot.RuleFor(s => s.RequiredScholars)
                    .GreaterThan(0).WithMessage("El número de becarios requeridos debe ser mayor a 0.");
            });
        });

        RuleForEach(x => x.SupervisorAssignments).ChildRules(assignment =>
        {
            assignment.RuleFor(a => a.SupervisorId)
                .NotEmpty().WithMessage("El identificador del supervisor es requerido.");

            assignment.RuleFor(a => a.CycleLocationId)
                .NotEmpty().WithMessage("El identificador de la ubicación del ciclo es requerido.");
        });
    }
}
