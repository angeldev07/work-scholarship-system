using FluentAssertions;
using WorkScholarship.Domain.Entities;

namespace WorkScholarship.Domain.Tests.Entities;

[Trait("Category", "Domain")]
[Trait("Entity", "ScheduleSlot")]
public class ScheduleSlotTests
{
    private static readonly Guid ValidCycleLocationId = Guid.NewGuid();
    private static readonly TimeOnly ValidStartTime = new(8, 0);
    private static readonly TimeOnly ValidEndTime = new(10, 0);

    // =====================================================================
    // ScheduleSlot.Create() — Factory Method
    // =====================================================================

    [Fact]
    public void Create_WithValidParameters_ReturnsSlotWithCorrectProperties()
    {
        // Act
        var slot = ScheduleSlot.Create(ValidCycleLocationId, 1, ValidStartTime, ValidEndTime, 2, "admin@test.com");

        // Assert
        slot.Should().NotBeNull();
        slot.CycleLocationId.Should().Be(ValidCycleLocationId);
        slot.DayOfWeek.Should().Be(1);
        slot.StartTime.Should().Be(ValidStartTime);
        slot.EndTime.Should().Be(ValidEndTime);
        slot.RequiredScholars.Should().Be(2);
        slot.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WithEmptyCycleLocationId_ThrowsArgumentException()
    {
        // Act
        var act = () => ScheduleSlot.Create(Guid.Empty, 1, ValidStartTime, ValidEndTime, 2, "admin@test.com");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*ubicación*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(-1)]
    public void Create_WithInvalidDayOfWeek_ThrowsArgumentOutOfRangeException(int dayOfWeek)
    {
        // Act
        var act = () => ScheduleSlot.Create(ValidCycleLocationId, dayOfWeek, ValidStartTime, ValidEndTime, 2, "admin@test.com");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WhenStartTimeAfterEndTime_ThrowsArgumentException()
    {
        // Arrange
        var startTime = new TimeOnly(10, 0);
        var endTime = new TimeOnly(8, 0);

        // Act
        var act = () => ScheduleSlot.Create(ValidCycleLocationId, 1, startTime, endTime, 2, "admin@test.com");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*inicio*");
    }

    [Fact]
    public void Create_WhenStartTimeEqualsEndTime_ThrowsArgumentException()
    {
        // Arrange
        var time = new TimeOnly(8, 0);

        // Act
        var act = () => ScheduleSlot.Create(ValidCycleLocationId, 1, time, time, 2, "admin@test.com");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithZeroOrNegativeRequiredScholars_ThrowsArgumentOutOfRangeException(int requiredScholars)
    {
        // Act
        var act = () => ScheduleSlot.Create(ValidCycleLocationId, 1, ValidStartTime, ValidEndTime, requiredScholars, "admin@test.com");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // =====================================================================
    // DayOfWeek válidos (1 al 7)
    // =====================================================================

    [Theory]
    [InlineData(1)] // Lunes
    [InlineData(2)] // Martes
    [InlineData(3)] // Miércoles
    [InlineData(4)] // Jueves
    [InlineData(5)] // Viernes
    [InlineData(6)] // Sábado
    [InlineData(7)] // Domingo
    public void Create_WithValidDayOfWeek_Succeeds(int dayOfWeek)
    {
        // Act
        var slot = ScheduleSlot.Create(ValidCycleLocationId, dayOfWeek, ValidStartTime, ValidEndTime, 2, "admin@test.com");

        // Assert
        slot.DayOfWeek.Should().Be(dayOfWeek);
    }

    // =====================================================================
    // DurationInHours — Computed property
    // =====================================================================

    [Fact]
    public void DurationInHours_CalculatesCorrectly()
    {
        // Arrange
        var startTime = new TimeOnly(8, 0);
        var endTime = new TimeOnly(10, 0);
        var slot = ScheduleSlot.Create(ValidCycleLocationId, 1, startTime, endTime, 2, "admin@test.com");

        // Assert
        slot.DurationInHours.Should().Be(2.0);
    }

    [Fact]
    public void DurationInHours_WithHalfHourSlot_ReturnsPointFive()
    {
        // Arrange
        var startTime = new TimeOnly(8, 0);
        var endTime = new TimeOnly(8, 30);
        var slot = ScheduleSlot.Create(ValidCycleLocationId, 1, startTime, endTime, 1, "admin@test.com");

        // Assert
        slot.DurationInHours.Should().Be(0.5);
    }

    // =====================================================================
    // DayOfWeekName — Computed property
    // =====================================================================

    [Theory]
    [InlineData(1, "Lunes")]
    [InlineData(2, "Martes")]
    [InlineData(3, "Miércoles")]
    [InlineData(4, "Jueves")]
    [InlineData(5, "Viernes")]
    [InlineData(6, "Sábado")]
    [InlineData(7, "Domingo")]
    public void DayOfWeekName_ReturnsCorrectSpanishName(int dayOfWeek, string expectedName)
    {
        // Arrange
        var slot = ScheduleSlot.Create(ValidCycleLocationId, dayOfWeek, ValidStartTime, ValidEndTime, 2, "admin@test.com");

        // Assert
        slot.DayOfWeekName.Should().Be(expectedName);
    }

    // =====================================================================
    // ScheduleSlot.Update()
    // =====================================================================

    [Fact]
    public void Update_WithValidParameters_UpdatesProperties()
    {
        // Arrange
        var slot = ScheduleSlot.Create(ValidCycleLocationId, 1, ValidStartTime, ValidEndTime, 2, "admin@test.com");
        var newStart = new TimeOnly(9, 0);
        var newEnd = new TimeOnly(11, 0);

        // Act
        slot.Update(newStart, newEnd, 3, "admin@test.com");

        // Assert
        slot.StartTime.Should().Be(newStart);
        slot.EndTime.Should().Be(newEnd);
        slot.RequiredScholars.Should().Be(3);
        slot.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Update_WithInvalidTimes_ThrowsArgumentException()
    {
        // Arrange
        var slot = ScheduleSlot.Create(ValidCycleLocationId, 1, ValidStartTime, ValidEndTime, 2, "admin@test.com");

        // Act
        var act = () => slot.Update(new TimeOnly(10, 0), new TimeOnly(8, 0), 2, "admin@test.com");

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
