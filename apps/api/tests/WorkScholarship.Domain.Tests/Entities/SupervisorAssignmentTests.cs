using FluentAssertions;
using WorkScholarship.Domain.Entities;

namespace WorkScholarship.Domain.Tests.Entities;

[Trait("Category", "Domain")]
[Trait("Entity", "SupervisorAssignment")]
public class SupervisorAssignmentTests
{
    private static readonly Guid ValidCycleId = Guid.NewGuid();
    private static readonly Guid ValidCycleLocationId = Guid.NewGuid();
    private static readonly Guid ValidSupervisorId = Guid.NewGuid();

    // =====================================================================
    // SupervisorAssignment.Create() — Factory Method
    // =====================================================================

    [Fact]
    public void Create_WithValidParameters_ReturnsAssignmentWithCorrectProperties()
    {
        // Act
        var assignment = SupervisorAssignment.Create(
            ValidCycleId, ValidCycleLocationId, ValidSupervisorId, "admin@test.com");

        // Assert
        assignment.Should().NotBeNull();
        assignment.CycleId.Should().Be(ValidCycleId);
        assignment.CycleLocationId.Should().Be(ValidCycleLocationId);
        assignment.SupervisorId.Should().Be(ValidSupervisorId);
        assignment.AssignedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        assignment.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WithEmptyCycleId_ThrowsArgumentException()
    {
        // Act
        var act = () => SupervisorAssignment.Create(
            Guid.Empty, ValidCycleLocationId, ValidSupervisorId, "admin@test.com");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*ciclo*");
    }

    [Fact]
    public void Create_WithEmptyCycleLocationId_ThrowsArgumentException()
    {
        // Act
        var act = () => SupervisorAssignment.Create(
            ValidCycleId, Guid.Empty, ValidSupervisorId, "admin@test.com");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*ubicación*");
    }

    [Fact]
    public void Create_WithEmptySupervisorId_ThrowsArgumentException()
    {
        // Act
        var act = () => SupervisorAssignment.Create(
            ValidCycleId, ValidCycleLocationId, Guid.Empty, "admin@test.com");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*supervisor*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyCreatedBy_ThrowsArgumentException(string? createdBy)
    {
        // Act
        var act = () => SupervisorAssignment.Create(
            ValidCycleId, ValidCycleLocationId, ValidSupervisorId, createdBy!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_TwoDifferentAssignments_HaveDifferentIds()
    {
        // Act
        var assignment1 = SupervisorAssignment.Create(ValidCycleId, ValidCycleLocationId, ValidSupervisorId, "admin@test.com");
        var assignment2 = SupervisorAssignment.Create(ValidCycleId, ValidCycleLocationId, ValidSupervisorId, "admin@test.com");

        // Assert
        assignment1.Id.Should().NotBe(assignment2.Id);
    }
}
