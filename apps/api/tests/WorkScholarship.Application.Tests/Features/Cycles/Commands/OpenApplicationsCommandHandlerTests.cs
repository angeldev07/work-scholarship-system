using FluentAssertions;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Cycles.Commands.OpenApplications;
using WorkScholarship.Application.Tests.TestHelpers;
using WorkScholarship.Domain.Entities;
using WorkScholarship.Domain.Enums;
using WorkScholarship.Infrastructure.Data;

namespace WorkScholarship.Application.Tests.Features.Cycles.Commands;

[Trait("Category", "Application")]
[Trait("Feature", "Cycles")]
[Trait("Component", "OpenApplicationsCommandHandler")]
public class OpenApplicationsCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly OpenApplicationsCommandHandler _handler;

    private static readonly DateTime _now = DateTime.UtcNow;
    private static readonly DateTime _startDate = _now.AddDays(30);
    private static readonly DateTime _endDate = _now.AddDays(180);
    private static readonly DateTime _applicationDeadline = _now.AddDays(40);
    private static readonly DateTime _interviewDate = _now.AddDays(50);
    private static readonly DateTime _selectionDate = _now.AddDays(60);

    public OpenApplicationsCommandHandlerTests()
    {
        _dbContext = DbContextFactory.CreateInMemoryContext();
        _handler = new OpenApplicationsCommandHandler(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    /// <summary>
    /// Crea un ciclo en estado Configuration con RenewalProcessCompleted = true (primer ciclo).
    /// </summary>
    private Cycle CreateCycleReadyToOpen(string department = "Biblioteca")
    {
        var cycle = Cycle.Create(
            "2024-2", department, _startDate, _endDate,
            _applicationDeadline, _interviewDate, _selectionDate, 10, "admin@test.com");

        // Marcar renovaciones como completadas para que el ciclo pueda abrir postulaciones
        cycle.MarkRenewalProcessCompleted();
        _dbContext.Cycles.Add(cycle);
        return cycle;
    }

    /// <summary>
    /// Agrega una CycleLocation activa al ciclo dado.
    /// </summary>
    private CycleLocation AddActiveLocation(Guid cycleId)
    {
        var location = CycleLocation.Create(cycleId, Guid.NewGuid(), 3, "admin@test.com");
        _dbContext.CycleLocations.Add(location);
        return location;
    }

    // =====================================================================
    // Happy path
    // =====================================================================

    [Fact]
    public async Task Handle_WithValidCycleAndLocations_ReturnsSuccessWithApplicationsOpenStatus()
    {
        // Arrange
        var cycle = CreateCycleReadyToOpen();
        AddActiveLocation(cycle.Id);
        await _dbContext.SaveChangesAsync();

        var command = new OpenApplicationsCommand(cycle.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(CycleStatus.ApplicationsOpen);
    }

    [Fact]
    public async Task Handle_WithValidCycle_ReturnsCycleDtoWithCorrectCounts()
    {
        // Arrange
        var cycle = CreateCycleReadyToOpen();
        AddActiveLocation(cycle.Id);
        AddActiveLocation(cycle.Id);
        await _dbContext.SaveChangesAsync();

        var command = new OpenApplicationsCommand(cycle.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.LocationsCount.Should().Be(2);
        result.Value.Id.Should().Be(cycle.Id);
    }

    [Fact]
    public async Task Handle_WithValidCycle_PersistsStatusChangeToDatabase()
    {
        // Arrange
        var cycle = CreateCycleReadyToOpen();
        AddActiveLocation(cycle.Id);
        await _dbContext.SaveChangesAsync();

        var command = new OpenApplicationsCommand(cycle.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — verificar que el estado persistió en la BD
        var savedCycle = await _dbContext.Cycles.FindAsync(cycle.Id);
        savedCycle!.Status.Should().Be(CycleStatus.ApplicationsOpen);
    }

    // =====================================================================
    // Error: ciclo no encontrado
    // =====================================================================

    [Fact]
    public async Task Handle_WithNonExistentCycleId_ReturnsCycleNotFoundFailure()
    {
        // Arrange
        var command = new OpenApplicationsCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be($"{CycleAppError.CYCLE_NOT_FOUND}");
        result.Error.Message.Should().NotBeNullOrWhiteSpace();
    }

    // =====================================================================
    // Error: transición inválida (estado incorrecto)
    // =====================================================================

    [Fact]
    public async Task Handle_WhenCycleAlreadyInApplicationsOpen_ReturnsInvalidTransitionFailure()
    {
        // Arrange — el ciclo debe primero pasar a ApplicationsOpen
        var cycle = CreateCycleReadyToOpen();
        AddActiveLocation(cycle.Id);
        await _dbContext.SaveChangesAsync();

        // Abrir una primera vez para que quede en ApplicationsOpen
        var firstOpen = new OpenApplicationsCommand(cycle.Id);
        await _handler.Handle(firstOpen, CancellationToken.None);

        // Act — intentar abrir de nuevo desde ApplicationsOpen (inválido)
        var command = new OpenApplicationsCommand(cycle.Id);
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be($"{CycleAppError.INVALID_TRANSITION}");
    }

    // =====================================================================
    // Error: sin ubicaciones activas
    // =====================================================================

    [Fact]
    public async Task Handle_WithNoCycleLocations_ReturnsNoLocationsFailure()
    {
        // Arrange — ciclo sin ubicaciones activas
        var cycle = CreateCycleReadyToOpen();
        await _dbContext.SaveChangesAsync();

        var command = new OpenApplicationsCommand(cycle.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be($"{CycleAppError.NO_LOCATIONS}");
    }

    // =====================================================================
    // Error: renovaciones pendientes
    // =====================================================================

    [Fact]
    public async Task Handle_WithRenewalsPending_ReturnsRenewalsPendingFailure()
    {
        // Arrange — ciclo con ubicación pero sin marcar renovaciones completadas
        var cycle = Cycle.Create(
            "2024-2", "Biblioteca", _startDate, _endDate,
            _applicationDeadline, _interviewDate, _selectionDate, 10, "admin@test.com");
        // NO llamar MarkRenewalProcessCompleted()
        _dbContext.Cycles.Add(cycle);
        AddActiveLocation(cycle.Id);
        await _dbContext.SaveChangesAsync();

        var command = new OpenApplicationsCommand(cycle.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be($"{CycleAppError.RENEWALS_PENDING}");
    }
}
