using FluentAssertions;
using WorkScholarship.Application.Features.Cycles.Commands.ExtendDates;

namespace WorkScholarship.Application.Tests.Features.Cycles.Commands;

[Trait("Category", "Application")]
[Trait("Feature", "Cycles")]
[Trait("Component", "ExtendCycleDatesCommandValidator")]
public class ExtendCycleDatesCommandValidatorTests
{
    private readonly ExtendCycleDatesCommandValidator _validator;

    private static readonly DateTime _now = DateTime.UtcNow;

    public ExtendCycleDatesCommandValidatorTests()
    {
        _validator = new ExtendCycleDatesCommandValidator();
    }

    // =====================================================================
    // Happy path
    // =====================================================================

    [Fact]
    public async Task Validate_WithAtLeastOneDateProvided_ShouldBeValid()
    {
        // Arrange — solo se provee NewEndDate; el resto es nulo
        var command = new ExtendCycleDatesCommand
        {
            CycleId = Guid.NewGuid(),
            NewEndDate = _now.AddDays(200)
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithAllDatesProvided_ShouldBeValid()
    {
        // Arrange — todas las fechas opcionales son proporcionadas
        var command = new ExtendCycleDatesCommand
        {
            CycleId = Guid.NewGuid(),
            NewApplicationDeadline = _now.AddDays(50),
            NewInterviewDate = _now.AddDays(60),
            NewSelectionDate = _now.AddDays(70),
            NewEndDate = _now.AddDays(200)
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    // =====================================================================
    // Error: CycleId vacío
    // =====================================================================

    [Fact]
    public async Task Validate_WithEmptyCycleId_ShouldFailWithCycleIdError()
    {
        // Arrange
        var command = new ExtendCycleDatesCommand
        {
            CycleId = Guid.Empty,
            NewEndDate = _now.AddDays(200)
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ExtendCycleDatesCommand.CycleId));
    }

    // =====================================================================
    // Error: ninguna fecha proporcionada
    // =====================================================================

    [Fact]
    public async Task Validate_WithNoDateProvided_ShouldFailWithAtLeastOneDateError()
    {
        // Arrange — todas las fechas son nulas
        var command = new ExtendCycleDatesCommand
        {
            CycleId = Guid.NewGuid()
            // NewApplicationDeadline, NewInterviewDate, NewSelectionDate, NewEndDate todos null
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage.Contains("al menos una fecha"));
    }
}
