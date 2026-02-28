using FluentAssertions;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Cycles.Commands.CloseApplications;
using WorkScholarship.Application.Features.Cycles.Commands.CloseCycle;
using WorkScholarship.Application.Features.Cycles.Commands.OpenApplications;
using WorkScholarship.Application.Tests.TestHelpers;
using WorkScholarship.Domain.Entities;
using WorkScholarship.Domain.Enums;
using WorkScholarship.Infrastructure.Data;

namespace WorkScholarship.Application.Tests.Features.Cycles.Commands;

[Trait("Category", "Application")]
[Trait("Feature", "Cycles")]
[Trait("Component", "CloseCycleCommandHandler")]
public class CloseCycleCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly CloseCycleCommandHandler _handler;
    private readonly OpenApplicationsCommandHandler _openHandler;
    private readonly CloseApplicationsCommandHandler _closeApplicationsHandler;

    private static readonly DateTime _now = DateTime.UtcNow;

    // Fechas con EndDate en el PASADO para permitir el cierre del ciclo
    private static readonly DateTime _pastStartDate = _now.AddDays(-180);
    private static readonly DateTime _pastEndDate = _now.AddDays(-1);
    private static readonly DateTime _pastApplicationDeadline = _now.AddDays(-160);
    private static readonly DateTime _pastInterviewDate = _now.AddDays(-150);
    private static readonly DateTime _pastSelectionDate = _now.AddDays(-140);

    // Fechas con EndDate en el FUTURO (para test de CYCLE_NOT_ENDED)
    private static readonly DateTime _futureStartDate = _now.AddDays(30);
    private static readonly DateTime _futureEndDate = _now.AddDays(180);
    private static readonly DateTime _futureApplicationDeadline = _now.AddDays(40);
    private static readonly DateTime _futureInterviewDate = _now.AddDays(50);
    private static readonly DateTime _futureSelectionDate = _now.AddDays(60);

    public CloseCycleCommandHandlerTests()
    {
        _dbContext = DbContextFactory.CreateInMemoryContext();
        _handler = new CloseCycleCommandHandler(_dbContext);
        _openHandler = new OpenApplicationsCommandHandler(_dbContext);
        _closeApplicationsHandler = new CloseApplicationsCommandHandler(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    /// <summary>
    /// Crea un ciclo con fechas en el pasado y lo lleva al estado Active listo para cerrar.
    /// Cadena de transiciones: Configuration → OpenApplications → CloseApplications → Activate (dominio directo).
    /// </summary>
    private async Task<Cycle> CreateCycleInActiveStateWithPastDatesAsync(string department = "Biblioteca")
    {
        var cycle = Cycle.Create(
            "2024-1", department, _pastStartDate, _pastEndDate,
            _pastApplicationDeadline, _pastInterviewDate, _pastSelectionDate, 10, "admin@test.com");

        cycle.MarkRenewalProcessCompleted();
        _dbContext.Cycles.Add(cycle);

        var location = CycleLocation.Create(cycle.Id, Guid.NewGuid(), 3, "admin@test.com");
        _dbContext.CycleLocations.Add(location);
        await _dbContext.SaveChangesAsync();

        await _openHandler.Handle(new OpenApplicationsCommand(cycle.Id), CancellationToken.None);
        await _closeApplicationsHandler.Handle(new CloseApplicationsCommand(cycle.Id), CancellationToken.None);

        // Activar directamente desde el dominio (no existe ActivateCommandHandler aún)
        cycle.Activate();
        await _dbContext.SaveChangesAsync();

        return cycle;
    }

    /// <summary>
    /// Crea un ciclo con fechas en el futuro y lo lleva al estado Active (EndDate no ha llegado).
    /// </summary>
    private async Task<Cycle> CreateCycleInActiveStateWithFutureDatesAsync(string department = "Biblioteca")
    {
        var cycle = Cycle.Create(
            "2024-2", department, _futureStartDate, _futureEndDate,
            _futureApplicationDeadline, _futureInterviewDate, _futureSelectionDate, 10, "admin@test.com");

        cycle.MarkRenewalProcessCompleted();
        _dbContext.Cycles.Add(cycle);

        var location = CycleLocation.Create(cycle.Id, Guid.NewGuid(), 3, "admin@test.com");
        _dbContext.CycleLocations.Add(location);
        await _dbContext.SaveChangesAsync();

        await _openHandler.Handle(new OpenApplicationsCommand(cycle.Id), CancellationToken.None);
        await _closeApplicationsHandler.Handle(new CloseApplicationsCommand(cycle.Id), CancellationToken.None);

        // Activar directamente desde el dominio (no existe ActivateCommandHandler aún)
        cycle.Activate();
        await _dbContext.SaveChangesAsync();

        return cycle;
    }

    // =====================================================================
    // Happy path
    // =====================================================================

    [Fact]
    public async Task Handle_WithActiveCycleAndPastEndDate_ReturnsSuccessWithClosedStatus()
    {
        // Arrange
        var cycle = await CreateCycleInActiveStateWithPastDatesAsync();

        var command = new CloseCycleCommand(cycle.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(CycleStatus.Closed);
    }

    [Fact]
    public async Task Handle_WithValidCycle_PersistsStatusChangeToDatabase()
    {
        // Arrange
        var cycle = await CreateCycleInActiveStateWithPastDatesAsync();

        var command = new CloseCycleCommand(cycle.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var savedCycle = await _dbContext.Cycles.FindAsync(cycle.Id);
        savedCycle!.Status.Should().Be(CycleStatus.Closed);
    }

    [Fact]
    public async Task Handle_WithValidCycle_ReturnsCycleDtoWithCorrectId()
    {
        // Arrange
        var cycle = await CreateCycleInActiveStateWithPastDatesAsync();

        var command = new CloseCycleCommand(cycle.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(cycle.Id);
    }

    [Fact]
    public async Task Handle_WithValidCycle_SetsClosedAt()
    {
        // Arrange
        var cycle = await CreateCycleInActiveStateWithPastDatesAsync();
        var beforeClose = DateTime.UtcNow;

        var command = new CloseCycleCommand(cycle.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ClosedAt.Should().NotBeNull();
        result.Value.ClosedAt.Should().BeOnOrAfter(beforeClose);
    }

    // =====================================================================
    // Error: ciclo no encontrado
    // =====================================================================

    [Fact]
    public async Task Handle_WithNonExistentCycleId_ReturnsCycleNotFoundFailure()
    {
        // Arrange
        var command = new CloseCycleCommand(Guid.NewGuid());

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
    public async Task Handle_WhenCycleIsInConfiguration_ReturnsInvalidTransitionFailure()
    {
        // Arrange — ciclo en Configuration (estado inicial)
        var cycle = Cycle.Create(
            "2024-1", "Biblioteca", _pastStartDate, _pastEndDate,
            _pastApplicationDeadline, _pastInterviewDate, _pastSelectionDate, 10, "admin@test.com");
        cycle.MarkRenewalProcessCompleted();
        _dbContext.Cycles.Add(cycle);
        await _dbContext.SaveChangesAsync();

        var command = new CloseCycleCommand(cycle.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be($"{CycleAppError.INVALID_TRANSITION}");
    }

    [Fact]
    public async Task Handle_WhenCycleIsInApplicationsOpen_ReturnsInvalidTransitionFailure()
    {
        // Arrange — ciclo en ApplicationsOpen
        var cycle = Cycle.Create(
            "2024-1", "Biblioteca", _pastStartDate, _pastEndDate,
            _pastApplicationDeadline, _pastInterviewDate, _pastSelectionDate, 10, "admin@test.com");
        cycle.MarkRenewalProcessCompleted();
        _dbContext.Cycles.Add(cycle);

        var location = CycleLocation.Create(cycle.Id, Guid.NewGuid(), 3, "admin@test.com");
        _dbContext.CycleLocations.Add(location);
        await _dbContext.SaveChangesAsync();

        await _openHandler.Handle(new OpenApplicationsCommand(cycle.Id), CancellationToken.None);

        var command = new CloseCycleCommand(cycle.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be($"{CycleAppError.INVALID_TRANSITION}");
    }

    [Fact]
    public async Task Handle_WhenCycleIsActiveButEndDateInFuture_ReturnsCycleNotEndedFailure()
    {
        // Arrange — ciclo en Active con EndDate en el futuro (aún no ha terminado)
        var cycle = await CreateCycleInActiveStateWithFutureDatesAsync();

        var command = new CloseCycleCommand(cycle.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be($"{CycleAppError.CYCLE_NOT_ENDED}");
    }
}
