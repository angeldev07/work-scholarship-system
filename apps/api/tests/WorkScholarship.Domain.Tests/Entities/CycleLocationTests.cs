using FluentAssertions;
using WorkScholarship.Domain.Entities;

namespace WorkScholarship.Domain.Tests.Entities;

[Trait("Category", "Domain")]
[Trait("Entity", "CycleLocation")]
public class CycleLocationTests
{
    private static readonly Guid ValidCycleId = Guid.NewGuid();
    private static readonly Guid ValidLocationId = Guid.NewGuid();

    // =====================================================================
    // CycleLocation.Create() — Factory Method
    // =====================================================================

    [Fact]
    public void Create_WithValidParameters_ReturnsCycleLocationWithCorrectProperties()
    {
        // Act
        var cycleLocation = CycleLocation.Create(ValidCycleId, ValidLocationId, 5, "admin@test.com");

        // Assert
        cycleLocation.Should().NotBeNull();
        cycleLocation.CycleId.Should().Be(ValidCycleId);
        cycleLocation.LocationId.Should().Be(ValidLocationId);
        cycleLocation.ScholarshipsAvailable.Should().Be(5);
        cycleLocation.ScholarshipsAssigned.Should().Be(0);
        cycleLocation.IsActive.Should().BeTrue();
        cycleLocation.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WithEmptyCycleId_ThrowsArgumentException()
    {
        // Act
        var act = () => CycleLocation.Create(Guid.Empty, ValidLocationId, 5, "admin@test.com");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*ciclo*");
    }

    [Fact]
    public void Create_WithEmptyLocationId_ThrowsArgumentException()
    {
        // Act
        var act = () => CycleLocation.Create(ValidCycleId, Guid.Empty, 5, "admin@test.com");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*ubicación*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Create_WithZeroOrNegativeScholarships_ThrowsArgumentOutOfRangeException(int scholarships)
    {
        // Act
        var act = () => CycleLocation.Create(ValidCycleId, ValidLocationId, scholarships, "admin@test.com");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // =====================================================================
    // CycleLocation.Deactivate() / Activate()
    // =====================================================================

    [Fact]
    public void Deactivate_ActiveCycleLocation_SetsIsActiveToFalse()
    {
        // Arrange
        var cycleLocation = CycleLocation.Create(ValidCycleId, ValidLocationId, 3, "admin@test.com");

        // Act
        cycleLocation.Deactivate("admin@test.com");

        // Assert
        cycleLocation.IsActive.Should().BeFalse();
        cycleLocation.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Activate_InactiveCycleLocation_SetsIsActiveToTrue()
    {
        // Arrange
        var cycleLocation = CycleLocation.Create(ValidCycleId, ValidLocationId, 3, "admin@test.com");
        cycleLocation.Deactivate("admin@test.com");

        // Act
        cycleLocation.Activate("admin@test.com");

        // Assert
        cycleLocation.IsActive.Should().BeTrue();
    }

    // =====================================================================
    // CycleLocation.IncrementAssignedScholarships()
    // =====================================================================

    [Fact]
    public void IncrementAssignedScholarships_IncrementsCorrectly()
    {
        // Arrange
        var cycleLocation = CycleLocation.Create(ValidCycleId, ValidLocationId, 5, "admin@test.com");

        // Act
        cycleLocation.IncrementAssignedScholarships(2);

        // Assert
        cycleLocation.ScholarshipsAssigned.Should().Be(2);
    }

    // =====================================================================
    // Query Methods — HasAvailableSlots / RemainingSlots
    // =====================================================================

    [Fact]
    public void HasAvailableSlots_WhenNotFull_ReturnsTrue()
    {
        // Arrange
        var cycleLocation = CycleLocation.Create(ValidCycleId, ValidLocationId, 5, "admin@test.com");
        cycleLocation.IncrementAssignedScholarships(3);

        // Assert
        cycleLocation.HasAvailableSlots.Should().BeTrue();
        cycleLocation.RemainingSlots.Should().Be(2);
    }

    [Fact]
    public void HasAvailableSlots_WhenFull_ReturnsFalse()
    {
        // Arrange
        var cycleLocation = CycleLocation.Create(ValidCycleId, ValidLocationId, 3, "admin@test.com");
        cycleLocation.IncrementAssignedScholarships(3);

        // Assert
        cycleLocation.HasAvailableSlots.Should().BeFalse();
        cycleLocation.RemainingSlots.Should().Be(0);
    }

    // =====================================================================
    // CycleLocation.UpdateScholarshipsAvailable()
    // =====================================================================

    [Fact]
    public void UpdateScholarshipsAvailable_WithValidValue_UpdatesProperty()
    {
        // Arrange
        var cycleLocation = CycleLocation.Create(ValidCycleId, ValidLocationId, 3, "admin@test.com");

        // Act
        cycleLocation.UpdateScholarshipsAvailable(7, "admin@test.com");

        // Assert
        cycleLocation.ScholarshipsAvailable.Should().Be(7);
        cycleLocation.UpdatedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void UpdateScholarshipsAvailable_WithInvalidValue_ThrowsArgumentOutOfRangeException(int invalidValue)
    {
        // Arrange
        var cycleLocation = CycleLocation.Create(ValidCycleId, ValidLocationId, 3, "admin@test.com");

        // Act
        var act = () => cycleLocation.UpdateScholarshipsAvailable(invalidValue, "admin@test.com");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
