using FluentAssertions;
using FluentValidation.TestHelper;
using WorkScholarship.Application.Features.Cycles.Commands.CreateCycle;

namespace WorkScholarship.Application.Tests.Features.Cycles.Commands;

[Trait("Category", "Application")]
[Trait("Feature", "Cycles")]
[Trait("Component", "CreateCycleCommandValidator")]
public class CreateCycleCommandValidatorTests
{
    private readonly CreateCycleCommandValidator _validator = new();

    private static readonly DateTime _now = DateTime.UtcNow;
    private static readonly DateTime _futureStart = _now.AddDays(30);
    private static readonly DateTime _futureEnd = _now.AddDays(180);
    private static readonly DateTime _futureDeadline = _now.AddDays(40);
    private static readonly DateTime _futureInterview = _now.AddDays(50);
    private static readonly DateTime _futureSelection = _now.AddDays(60);

    private CreateCycleCommand BuildValidCommand() => new()
    {
        Name = "2024-2",
        Department = "Biblioteca",
        StartDate = _futureStart,
        EndDate = _futureEnd,
        ApplicationDeadline = _futureDeadline,
        InterviewDate = _futureInterview,
        SelectionDate = _futureSelection,
        TotalScholarshipsAvailable = 10
    };

    // =====================================================================
    // Name
    // =====================================================================

    [Fact]
    public void Validate_WithValidName_HasNoErrorForName()
    {
        var command = BuildValidCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Validate_WithEmptyName_HasValidationError(string? name)
    {
        var command = BuildValidCommand() with { Name = name! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithNameExceeding100Chars_HasValidationError()
    {
        var command = BuildValidCommand() with { Name = new string('A', 101) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithNameExactly100Chars_HasNoValidationError()
    {
        var command = BuildValidCommand() with { Name = new string('A', 100) };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    // =====================================================================
    // Department
    // =====================================================================

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Validate_WithEmptyDepartment_HasValidationError(string? department)
    {
        var command = BuildValidCommand() with { Department = department! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Department);
    }

    [Fact]
    public void Validate_WithDepartmentExceeding100Chars_HasValidationError()
    {
        var command = BuildValidCommand() with { Department = new string('B', 101) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Department);
    }

    // =====================================================================
    // StartDate
    // =====================================================================

    [Fact]
    public void Validate_WithStartDateInPast_HasValidationError()
    {
        var command = BuildValidCommand() with { StartDate = _now.AddDays(-1) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.StartDate);
    }

    [Fact]
    public void Validate_WithFutureStartDate_HasNoValidationError()
    {
        var command = BuildValidCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.StartDate);
    }

    // =====================================================================
    // EndDate
    // =====================================================================

    [Fact]
    public void Validate_WithEndDateBeforeStartDate_HasValidationError()
    {
        var command = BuildValidCommand() with { EndDate = _futureStart.AddDays(-1) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public void Validate_WithEndDateEqualToStartDate_HasValidationError()
    {
        var command = BuildValidCommand() with { EndDate = _futureStart };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    // =====================================================================
    // ApplicationDeadline
    // =====================================================================

    [Fact]
    public void Validate_WithApplicationDeadlineInPast_HasValidationError()
    {
        var command = BuildValidCommand() with { ApplicationDeadline = _now.AddDays(-1) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ApplicationDeadline);
    }

    [Fact]
    public void Validate_WithApplicationDeadlineAfterInterviewDate_HasValidationError()
    {
        var command = BuildValidCommand() with { ApplicationDeadline = _futureInterview.AddDays(1) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ApplicationDeadline);
    }

    // =====================================================================
    // InterviewDate
    // =====================================================================

    [Fact]
    public void Validate_WithInterviewDateAfterSelectionDate_HasValidationError()
    {
        var command = BuildValidCommand() with { InterviewDate = _futureSelection.AddDays(1) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.InterviewDate);
    }

    // =====================================================================
    // SelectionDate
    // =====================================================================

    [Fact]
    public void Validate_WithSelectionDateAfterEndDate_HasValidationError()
    {
        var command = BuildValidCommand() with { SelectionDate = _futureEnd.AddDays(1) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.SelectionDate);
    }

    // =====================================================================
    // TotalScholarshipsAvailable
    // =====================================================================

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WithNonPositiveScholarships_HasValidationError(int scholarships)
    {
        var command = BuildValidCommand() with { TotalScholarshipsAvailable = scholarships };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TotalScholarshipsAvailable);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void Validate_WithPositiveScholarships_HasNoValidationError(int scholarships)
    {
        var command = BuildValidCommand() with { TotalScholarshipsAvailable = scholarships };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.TotalScholarshipsAvailable);
    }

    // =====================================================================
    // Valid command
    // =====================================================================

    [Fact]
    public void Validate_WithValidCommand_HasNoValidationErrors()
    {
        var command = BuildValidCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
