using FluentAssertions;
using WorkScholarship.Domain.Common;
using WorkScholarship.Domain.Entities;
using WorkScholarship.Domain.Enums;
using WorkScholarship.Domain.Events;

namespace WorkScholarship.Domain.Tests.Entities;

[Trait("Category", "Domain")]
[Trait("Entity", "Cycle")]
public class CycleTests
{
    // =====================================================================
    // Datos de prueba reutilizables
    // =====================================================================

    private static readonly DateTime FutureStart = DateTime.UtcNow.AddDays(30);
    private static readonly DateTime FutureEnd = DateTime.UtcNow.AddDays(150);
    private static readonly DateTime FutureDeadline = DateTime.UtcNow.AddDays(45);
    private static readonly DateTime FutureInterview = DateTime.UtcNow.AddDays(52);
    private static readonly DateTime FutureSelection = DateTime.UtcNow.AddDays(59);

    private static Cycle CreateValidCycle(bool renewalCompleted = true)
    {
        var cycle = Cycle.Create(
            name: "2024-1",
            department: "Biblioteca",
            startDate: FutureStart,
            endDate: FutureEnd,
            applicationDeadline: FutureDeadline,
            interviewDate: FutureInterview,
            selectionDate: FutureSelection,
            totalScholarshipsAvailable: 20,
            createdBy: "admin@test.com");

        if (renewalCompleted)
            cycle.MarkRenewalProcessCompleted();

        return cycle;
    }

    // =====================================================================
    // Cycle.Create() — Factory Method
    // =====================================================================

    [Fact]
    public void Create_WithValidParameters_ReturnsCycleInConfigurationStatus()
    {
        // Act
        var cycle = Cycle.Create("2024-1", "Biblioteca", FutureStart, FutureEnd,
            FutureDeadline, FutureInterview, FutureSelection, 20, "admin@test.com");

        // Assert
        cycle.Should().NotBeNull();
        cycle.Status.Should().Be(CycleStatus.Configuration);
        cycle.Name.Should().Be("2024-1");
        cycle.Department.Should().Be("Biblioteca");
        cycle.TotalScholarshipsAvailable.Should().Be(20);
        cycle.TotalScholarshipsAssigned.Should().Be(0);
        cycle.RenewalProcessCompleted.Should().BeFalse();
        cycle.ClonedFromCycleId.Should().BeNull();
        cycle.ClosedAt.Should().BeNull();
        cycle.ClosedBy.Should().BeNull();
        cycle.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WithValidParameters_EmitsCycleCreatedEvent()
    {
        // Act
        var cycle = Cycle.Create("2024-1", "Biblioteca", FutureStart, FutureEnd,
            FutureDeadline, FutureInterview, FutureSelection, 20, "admin@test.com");

        // Assert
        cycle.DomainEvents.Should().ContainSingle(e => e is CycleCreatedEvent);
        var domainEvent = cycle.DomainEvents.OfType<CycleCreatedEvent>().First();
        domainEvent.CycleId.Should().Be(cycle.Id);
    }

    [Fact]
    public void Create_TrimsWhitespaceFromNameAndDepartment()
    {
        // Act
        var cycle = Cycle.Create("  2024-1  ", "  Biblioteca  ", FutureStart, FutureEnd,
            FutureDeadline, FutureInterview, FutureSelection, 20, "admin@test.com");

        // Assert
        cycle.Name.Should().Be("2024-1");
        cycle.Department.Should().Be("Biblioteca");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyName_ThrowsArgumentException(string? name)
    {
        // Act
        var act = () => Cycle.Create(name!, "Biblioteca", FutureStart, FutureEnd,
            FutureDeadline, FutureInterview, FutureSelection, 20, "admin@test.com");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*nombre*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyDepartment_ThrowsArgumentException(string? department)
    {
        // Act
        var act = () => Cycle.Create("2024-1", department!, FutureStart, FutureEnd,
            FutureDeadline, FutureInterview, FutureSelection, 20, "admin@test.com");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*departamento*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WithZeroOrNegativeScholarships_ThrowsArgumentOutOfRangeException(int totalScholarships)
    {
        // Act
        var act = () => Cycle.Create("2024-1", "Biblioteca", FutureStart, FutureEnd,
            FutureDeadline, FutureInterview, FutureSelection, totalScholarships, "admin@test.com");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WhenStartDateAfterEndDate_ThrowsArgumentException()
    {
        // Arrange
        var startDate = FutureEnd;
        var endDate = FutureStart;

        // Act
        var act = () => Cycle.Create("2024-1", "Biblioteca", startDate, endDate,
            FutureDeadline, FutureInterview, FutureSelection, 20, "admin@test.com");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WhenApplicationDeadlineAfterInterviewDate_ThrowsArgumentException()
    {
        // Arrange
        var deadline = FutureInterview.AddDays(1); // deadline > interviewDate → invalido

        // Act
        var act = () => Cycle.Create("2024-1", "Biblioteca", FutureStart, FutureEnd,
            deadline, FutureInterview, FutureSelection, 20, "admin@test.com");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WhenSelectionDateAfterEndDate_ThrowsArgumentException()
    {
        // Arrange
        var selectionDate = FutureEnd.AddDays(1); // selectionDate > endDate → invalido

        // Act
        var act = () => Cycle.Create("2024-1", "Biblioteca", FutureStart, FutureEnd,
            FutureDeadline, FutureInterview, selectionDate, 20, "admin@test.com");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    // =====================================================================
    // Cycle.OpenApplications() — Transición Configuration → ApplicationsOpen
    // =====================================================================

    [Fact]
    public void OpenApplications_FromConfiguration_WithLocations_ReturnsSuccess()
    {
        // Arrange
        var cycle = CreateValidCycle(renewalCompleted: true);

        // Act
        var result = cycle.OpenApplications(activeCycleLocationsCount: 3);

        // Assert
        result.IsSuccess.Should().BeTrue();
        cycle.Status.Should().Be(CycleStatus.ApplicationsOpen);
    }

    [Fact]
    public void OpenApplications_FromConfiguration_EmitsApplicationsOpenedEvent()
    {
        // Arrange
        var cycle = CreateValidCycle(renewalCompleted: true);
        cycle.ClearDomainEvents();

        // Act
        cycle.OpenApplications(activeCycleLocationsCount: 3);

        // Assert
        cycle.DomainEvents.Should().ContainSingle(e => e is ApplicationsOpenedEvent);
        var domainEvent = cycle.DomainEvents.OfType<ApplicationsOpenedEvent>().First();
        domainEvent.CycleId.Should().Be(cycle.Id);
    }

    [Fact]
    public void OpenApplications_FromNonConfigurationStatus_ReturnsFailure()
    {
        // Arrange
        var cycle = CreateValidCycle(renewalCompleted: true);
        cycle.OpenApplications(activeCycleLocationsCount: 3);
        // Ahora está en ApplicationsOpen

        // Act
        var result = cycle.OpenApplications(activeCycleLocationsCount: 3);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(CycleErrorCode.InvalidTransition);
    }

    [Fact]
    public void OpenApplications_WithNoLocations_ReturnsFailure()
    {
        // Arrange
        var cycle = CreateValidCycle(renewalCompleted: true);

        // Act
        var result = cycle.OpenApplications(activeCycleLocationsCount: 0);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(CycleErrorCode.NoLocations);
    }

    [Fact]
    public void OpenApplications_WhenRenewalNotCompleted_ReturnsFailure()
    {
        // Arrange
        var cycle = CreateValidCycle(renewalCompleted: false); // RenewalProcessCompleted = false

        // Act
        var result = cycle.OpenApplications(activeCycleLocationsCount: 3);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(CycleErrorCode.RenewalsPending);
    }

    // =====================================================================
    // Cycle.CloseApplications() — Transición ApplicationsOpen → ApplicationsClosed
    // =====================================================================

    [Fact]
    public void CloseApplications_FromApplicationsOpen_ReturnsSuccess()
    {
        // Arrange
        var cycle = CreateValidCycle(renewalCompleted: true);
        cycle.OpenApplications(activeCycleLocationsCount: 3);

        // Act
        var result = cycle.CloseApplications();

        // Assert
        result.IsSuccess.Should().BeTrue();
        cycle.Status.Should().Be(CycleStatus.ApplicationsClosed);
    }

    [Fact]
    public void CloseApplications_FromApplicationsOpen_EmitsApplicationsClosedEvent()
    {
        // Arrange
        var cycle = CreateValidCycle(renewalCompleted: true);
        cycle.OpenApplications(activeCycleLocationsCount: 3);
        cycle.ClearDomainEvents();

        // Act
        cycle.CloseApplications();

        // Assert
        cycle.DomainEvents.Should().ContainSingle(e => e is ApplicationsClosedEvent);
    }

    [Fact]
    public void CloseApplications_FromConfiguration_ReturnsFailure()
    {
        // Arrange
        var cycle = CreateValidCycle();

        // Act
        var result = cycle.CloseApplications();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(CycleErrorCode.InvalidTransition);
    }

    // =====================================================================
    // Cycle.ReopenApplications() — Transición ApplicationsClosed → ApplicationsOpen
    // =====================================================================

    [Fact]
    public void ReopenApplications_FromApplicationsClosed_ReturnsSuccess()
    {
        // Arrange
        var cycle = CreateValidCycle(renewalCompleted: true);
        cycle.OpenApplications(activeCycleLocationsCount: 3);
        cycle.CloseApplications();

        // Act
        var result = cycle.ReopenApplications();

        // Assert
        result.IsSuccess.Should().BeTrue();
        cycle.Status.Should().Be(CycleStatus.ApplicationsOpen);
    }

    [Fact]
    public void ReopenApplications_FromApplicationsClosed_EmitsApplicationsReopenedEvent()
    {
        // Arrange
        var cycle = CreateValidCycle(renewalCompleted: true);
        cycle.OpenApplications(activeCycleLocationsCount: 3);
        cycle.CloseApplications();
        cycle.ClearDomainEvents();

        // Act
        cycle.ReopenApplications();

        // Assert
        cycle.DomainEvents.Should().ContainSingle(e => e is ApplicationsReopenedEvent);
    }

    [Fact]
    public void ReopenApplications_FromConfiguration_ReturnsFailure()
    {
        // Arrange
        var cycle = CreateValidCycle();

        // Act
        var result = cycle.ReopenApplications();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(CycleErrorCode.InvalidTransition);
    }

    // =====================================================================
    // Cycle.Activate() — Transición ApplicationsClosed → Active
    // =====================================================================

    [Fact]
    public void Activate_FromApplicationsClosed_ReturnsSuccess()
    {
        // Arrange
        var cycle = CreateValidCycle(renewalCompleted: true);
        cycle.OpenApplications(activeCycleLocationsCount: 3);
        cycle.CloseApplications();

        // Act
        var result = cycle.Activate();

        // Assert
        result.IsSuccess.Should().BeTrue();
        cycle.Status.Should().Be(CycleStatus.Active);
    }

    [Fact]
    public void Activate_FromApplicationsClosed_EmitsCycleActivatedEvent()
    {
        // Arrange
        var cycle = CreateValidCycle(renewalCompleted: true);
        cycle.OpenApplications(activeCycleLocationsCount: 3);
        cycle.CloseApplications();
        cycle.ClearDomainEvents();

        // Act
        cycle.Activate();

        // Assert
        cycle.DomainEvents.Should().ContainSingle(e => e is CycleActivatedEvent);
    }

    [Fact]
    public void Activate_FromConfiguration_ReturnsFailure()
    {
        // Arrange
        var cycle = CreateValidCycle();

        // Act
        var result = cycle.Activate();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(CycleErrorCode.InvalidTransition);
    }

    [Fact]
    public void Activate_FromApplicationsOpen_ReturnsFailure()
    {
        // Arrange
        var cycle = CreateValidCycle(renewalCompleted: true);
        cycle.OpenApplications(activeCycleLocationsCount: 3);

        // Act
        var result = cycle.Activate();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(CycleErrorCode.InvalidTransition);
    }

    // =====================================================================
    // Cycle.Close() — Transición Active → Closed
    // =====================================================================

    [Fact]
    public void Close_FromActiveWithPastEndDate_ReturnsSuccess()
    {
        // Arrange — Ciclo con EndDate en el pasado para poder cerrarlo
        var pastEndDate = DateTime.UtcNow.AddDays(-1);
        var cycle = CreateCycleWithPastEndDate(pastEndDate);

        // Act
        var result = cycle.Close(pendingShiftsCount: 0, missingLogbooksCount: 0, closedBy: "admin@test.com");

        // Assert
        result.IsSuccess.Should().BeTrue();
        cycle.Status.Should().Be(CycleStatus.Closed);
        cycle.ClosedAt.Should().NotBeNull();
        cycle.ClosedBy.Should().Be("admin@test.com");
    }

    [Fact]
    public void Close_FromActiveWithPastEndDate_EmitsCycleClosedEvent()
    {
        // Arrange
        var cycle = CreateCycleWithPastEndDate(DateTime.UtcNow.AddDays(-1));
        cycle.ClearDomainEvents();

        // Act
        cycle.Close(0, 0, "admin@test.com");

        // Assert
        cycle.DomainEvents.Should().ContainSingle(e => e is CycleClosedEvent);
    }

    [Fact]
    public void Close_WhenNotActive_ReturnsFailure()
    {
        // Arrange — Ciclo en Configuration
        var cycle = CreateValidCycle();

        // Act
        var result = cycle.Close(0, 0, "admin@test.com");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(CycleErrorCode.InvalidTransition);
    }

    [Fact]
    public void Close_WhenEndDateNotReached_ReturnsFailure()
    {
        // Arrange — Ciclo Active pero con EndDate en el futuro
        var cycle = CreateValidCycle(renewalCompleted: true);
        cycle.OpenApplications(activeCycleLocationsCount: 3);
        cycle.CloseApplications();
        cycle.Activate();
        // EndDate está en el futuro (FutureEnd = UtcNow + 150 días)

        // Act
        var result = cycle.Close(0, 0, "admin@test.com");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(CycleErrorCode.CycleNotEnded);
    }

    [Fact]
    public void Close_WithPendingShifts_ReturnsFailureWithCount()
    {
        // Arrange
        var cycle = CreateCycleWithPastEndDate(DateTime.UtcNow.AddDays(-1));

        // Act
        var result = cycle.Close(pendingShiftsCount: 5, missingLogbooksCount: 0, closedBy: "admin@test.com");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(CycleErrorCode.PendingShifts);
        result.ErrorMessage.Should().Contain("5");
    }

    [Fact]
    public void Close_WithMissingLogbooks_ReturnsFailureWithCount()
    {
        // Arrange
        var cycle = CreateCycleWithPastEndDate(DateTime.UtcNow.AddDays(-1));

        // Act
        var result = cycle.Close(pendingShiftsCount: 0, missingLogbooksCount: 3, closedBy: "admin@test.com");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(CycleErrorCode.MissingLogbooks);
        result.ErrorMessage.Should().Contain("3");
    }

    [Fact]
    public void Close_OnAlreadyClosedCycle_ReturnsFailure()
    {
        // Arrange
        var cycle = CreateCycleWithPastEndDate(DateTime.UtcNow.AddDays(-1));
        cycle.Close(0, 0, "admin@test.com");

        // Act
        var result = cycle.Close(0, 0, "admin@test.com");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(CycleErrorCode.InvalidTransition);
    }

    // =====================================================================
    // Cycle.ExtendDates() — Extensión de fechas
    // =====================================================================

    [Fact]
    public void ExtendDates_FromConfiguration_WithValidDates_ReturnsSuccess()
    {
        // Arrange
        var cycle = CreateValidCycle();
        var newEndDate = FutureEnd.AddDays(30);

        // Act
        var result = cycle.ExtendDates(null, null, null, newEndDate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        cycle.EndDate.Should().Be(newEndDate);
    }

    [Fact]
    public void ExtendDates_FromApplicationsOpen_ReturnsSuccess()
    {
        // Arrange
        var cycle = CreateValidCycle(renewalCompleted: true);
        cycle.OpenApplications(activeCycleLocationsCount: 3);
        var newEndDate = FutureEnd.AddDays(30);

        // Act
        var result = cycle.ExtendDates(null, null, null, newEndDate);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ExtendDates_FromApplicationsClosed_ReturnsFailure()
    {
        // Arrange
        var cycle = CreateValidCycle(renewalCompleted: true);
        cycle.OpenApplications(activeCycleLocationsCount: 3);
        cycle.CloseApplications();

        // Act
        var result = cycle.ExtendDates(null, null, null, FutureEnd.AddDays(30));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(CycleErrorCode.InvalidTransition);
    }

    [Fact]
    public void ExtendDates_FromClosed_ReturnsFailure()
    {
        // Arrange
        var cycle = CreateCycleWithPastEndDate(DateTime.UtcNow.AddDays(-1));
        cycle.Close(0, 0, "admin@test.com");

        // Act
        var result = cycle.ExtendDates(null, null, null, DateTime.UtcNow.AddDays(200));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(CycleErrorCode.CycleClosed);
    }

    [Fact]
    public void ExtendDates_WithSmallerEndDate_ReturnsFailure()
    {
        // Arrange
        var cycle = CreateValidCycle();
        var smallerEndDate = FutureEnd.AddDays(-1); // Menor que la actual

        // Act
        var result = cycle.ExtendDates(null, null, null, smallerEndDate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(CycleErrorCode.InvalidDate);
    }

    [Fact]
    public void ExtendDates_EmitsCycleDatesExtendedEvent()
    {
        // Arrange
        var cycle = CreateValidCycle();
        cycle.ClearDomainEvents();

        // Act
        cycle.ExtendDates(null, null, null, FutureEnd.AddDays(30));

        // Assert
        cycle.DomainEvents.Should().ContainSingle(e => e is CycleDatesExtendedEvent);
    }

    // =====================================================================
    // Query Methods (computed properties)
    // =====================================================================

    [Theory]
    [InlineData(CycleStatus.Configuration, true)]
    [InlineData(CycleStatus.ApplicationsOpen, true)]
    [InlineData(CycleStatus.ApplicationsClosed, true)]
    [InlineData(CycleStatus.Active, true)]
    [InlineData(CycleStatus.Closed, false)]
    public void IsModifiable_ReturnsExpectedValue(CycleStatus status, bool expected)
    {
        // Arrange
        var cycle = CreateCycleInStatus(status);

        // Assert
        cycle.IsModifiable.Should().Be(expected);
    }

    [Theory]
    [InlineData(CycleStatus.Configuration, false)]
    [InlineData(CycleStatus.ApplicationsOpen, true)]
    [InlineData(CycleStatus.ApplicationsClosed, false)]
    [InlineData(CycleStatus.Active, false)]
    [InlineData(CycleStatus.Closed, false)]
    public void AcceptsApplications_ReturnsExpectedValue(CycleStatus status, bool expected)
    {
        // Arrange
        var cycle = CreateCycleInStatus(status);

        // Assert
        cycle.AcceptsApplications.Should().Be(expected);
    }

    [Theory]
    [InlineData(CycleStatus.Configuration, false)]
    [InlineData(CycleStatus.ApplicationsOpen, false)]
    [InlineData(CycleStatus.ApplicationsClosed, false)]
    [InlineData(CycleStatus.Active, true)]
    [InlineData(CycleStatus.Closed, false)]
    public void IsOperational_ReturnsExpectedValue(CycleStatus status, bool expected)
    {
        // Arrange
        var cycle = CreateCycleInStatus(status);

        // Assert
        cycle.IsOperational.Should().Be(expected);
    }

    [Theory]
    [InlineData(CycleStatus.Configuration, false)]
    [InlineData(CycleStatus.Active, false)]
    [InlineData(CycleStatus.Closed, true)]
    public void IsClosed_ReturnsExpectedValue(CycleStatus status, bool expected)
    {
        // Arrange
        var cycle = CreateCycleInStatus(status);

        // Assert
        cycle.IsClosed.Should().Be(expected);
    }

    // =====================================================================
    // MarkRenewalProcessCompleted + SetClonedFromCycleId
    // =====================================================================

    [Fact]
    public void MarkRenewalProcessCompleted_SetsRenewalProcessCompletedToTrue()
    {
        // Arrange
        var cycle = CreateValidCycle(renewalCompleted: false);
        cycle.RenewalProcessCompleted.Should().BeFalse();

        // Act
        cycle.MarkRenewalProcessCompleted();

        // Assert
        cycle.RenewalProcessCompleted.Should().BeTrue();
    }

    [Fact]
    public void SetClonedFromCycleId_SetsClonedFromCycleId()
    {
        // Arrange
        var cycle = CreateValidCycle();
        var sourceCycleId = Guid.NewGuid();

        // Act
        cycle.SetClonedFromCycleId(sourceCycleId);

        // Assert
        cycle.ClonedFromCycleId.Should().Be(sourceCycleId);
    }

    // =====================================================================
    // IncrementAssignedScholarships
    // =====================================================================

    [Fact]
    public void IncrementAssignedScholarships_IncrementsCorrectly()
    {
        // Arrange
        var cycle = CreateValidCycle();

        // Act
        cycle.IncrementAssignedScholarships(5);

        // Assert
        cycle.TotalScholarshipsAssigned.Should().Be(5);
    }

    [Fact]
    public void IncrementAssignedScholarships_WithDefaultParameter_IncrementsByOne()
    {
        // Arrange
        var cycle = CreateValidCycle();

        // Act
        cycle.IncrementAssignedScholarships();

        // Assert
        cycle.TotalScholarshipsAssigned.Should().Be(1);
    }

    // =====================================================================
    // Helpers privados
    // =====================================================================

    /// <summary>
    /// Crea un ciclo con EndDate en el pasado para poder probar el cierre del ciclo.
    /// El ciclo se lleva hasta el estado Active.
    /// </summary>
    private static Cycle CreateCycleWithPastEndDate(DateTime pastEndDate)
    {
        // Crear con fechas pasadas para que Close() funcione (requiere UtcNow >= EndDate)
        var start = pastEndDate.AddDays(-120);
        var deadline = pastEndDate.AddDays(-90);
        var interview = pastEndDate.AddDays(-83);
        var selection = pastEndDate.AddDays(-76);

        var cycle = Cycle.Create("2023-2", "Biblioteca", start, pastEndDate,
            deadline, interview, selection, 20, "admin@test.com");

        cycle.MarkRenewalProcessCompleted();
        cycle.OpenApplications(activeCycleLocationsCount: 3);
        cycle.CloseApplications();
        cycle.Activate();

        return cycle;
    }

    /// <summary>
    /// Crea un ciclo en el estado especificado (helper para tests de query methods).
    /// </summary>
    private static Cycle CreateCycleInStatus(CycleStatus targetStatus)
    {
        return targetStatus switch
        {
            CycleStatus.Configuration => CreateValidCycle(),
            CycleStatus.ApplicationsOpen => CreateInApplicationsOpen(),
            CycleStatus.ApplicationsClosed => CreateInApplicationsClosed(),
            CycleStatus.Active => CreateInActive(),
            CycleStatus.Closed => CreateInClosed(),
            _ => throw new ArgumentOutOfRangeException(nameof(targetStatus))
        };
    }

    private static Cycle CreateInApplicationsOpen()
    {
        var cycle = CreateValidCycle(renewalCompleted: true);
        cycle.OpenApplications(activeCycleLocationsCount: 3);
        return cycle;
    }

    private static Cycle CreateInApplicationsClosed()
    {
        var cycle = CreateInApplicationsOpen();
        cycle.CloseApplications();
        return cycle;
    }

    private static Cycle CreateInActive()
    {
        var cycle = CreateInApplicationsClosed();
        cycle.Activate();
        return cycle;
    }

    private static Cycle CreateInClosed()
    {
        var cycle = CreateCycleWithPastEndDate(DateTime.UtcNow.AddDays(-1));
        cycle.Close(pendingShiftsCount: 0, missingLogbooksCount: 0, closedBy: "admin@test.com");
        return cycle;
    }
}
