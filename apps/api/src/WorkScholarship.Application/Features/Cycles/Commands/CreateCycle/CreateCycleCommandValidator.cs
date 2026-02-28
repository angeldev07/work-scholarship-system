using FluentValidation;

namespace WorkScholarship.Application.Features.Cycles.Commands.CreateCycle;

/// <summary>
/// Validador de FluentValidation para CreateCycleCommand.
/// </summary>
/// <remarks>
/// Aplica validaciones de formato e integridad de datos antes de que el handler procese el comando.
/// Las reglas de negocio de dominio (coherencia de fechas, RN-001) se verifican en el handler.
/// </remarks>
public class CreateCycleCommandValidator : AbstractValidator<CreateCycleCommand>
{
    /// <summary>
    /// Define las reglas de validación para el comando de creación de ciclo.
    /// </summary>
    public CreateCycleCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del ciclo es requerido.")
            .MaximumLength(100).WithMessage("El nombre del ciclo no puede superar 100 caracteres.");

        RuleFor(x => x.Department)
            .NotEmpty().WithMessage("El departamento es requerido.")
            .MaximumLength(100).WithMessage("El departamento no puede superar 100 caracteres.");

        RuleFor(x => x.StartDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("La fecha de inicio debe ser una fecha futura.");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate).WithMessage("La fecha de fin debe ser posterior a la fecha de inicio.");

        RuleFor(x => x.ApplicationDeadline)
            .GreaterThan(DateTime.UtcNow).WithMessage("La fecha límite de postulación debe ser una fecha futura.")
            .LessThan(x => x.InterviewDate).WithMessage("La fecha límite de postulación debe ser anterior a la fecha de entrevistas.");

        RuleFor(x => x.InterviewDate)
            .LessThan(x => x.SelectionDate).WithMessage("La fecha de entrevistas debe ser anterior a la fecha de selección.");

        RuleFor(x => x.SelectionDate)
            .LessThan(x => x.EndDate).WithMessage("La fecha de selección debe ser anterior a la fecha de fin del ciclo.");

        RuleFor(x => x.TotalScholarshipsAvailable)
            .GreaterThan(0).WithMessage("El total de becas disponibles debe ser mayor a 0.");
    }
}
