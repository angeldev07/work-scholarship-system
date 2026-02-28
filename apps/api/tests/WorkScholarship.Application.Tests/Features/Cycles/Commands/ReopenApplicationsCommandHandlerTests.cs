using FluentAssertions;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Cycles.Commands.CloseApplications;
using WorkScholarship.Application.Features.Cycles.Commands.OpenApplications;
using WorkScholarship.Application.Features.Cycles.Commands.ReopenApplications;
using WorkScholarship.Application.Tests.TestHelpers;
using WorkScholarship.Domain.Entities;
using WorkScholarship.Domain.Enums;
using WorkScholarship.Infrastructure.Data;

namespace WorkScholarship.Application.Tests.Features.Cycles.Commands;

[Trait("Category", "Application")]
[Trait("Feature", "Cycles")]
[Trait("Component", "ReopenApplicationsCommandHandler")]
public class ReopenApplicationsCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ReopenApplicationsCommandHandler _handler;
    private readonly OpenApplicationsCommandHandler _openHandler;
    private readonly CloseApplicationsCommandHandler _closeHandler;

    private static readonly DateTime _now = DateTime.UtcNow;
    private static readonly DateTime _startDate = _now.AddDays(30);
    private static readonly DateTime _endDate = _now.AddDays(180);
    private static readonly DateTime _applicationDeadline = _now.AddDays(40);
    private static readonly DateTime _interviewDate = _now.AddDays(50);
    private static readonly DateTime _selectionDate = _now.AddDays(60);

    public ReopenApplicationsCommandHandlerTests()
    {
        _dbContext = DbContextFactory.CreateInMemoryContext();
        _handler = new ReopenApplicationsCommandHandler(_dbContext);
        _openHandler = new OpenApplicationsCommandHandler(_dbContext);
        _closeHandler = new CloseApplicationsCommandHandler(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    /// <summary>
    /// Crea un ciclo y lo lleva al estado ApplicationsClosed (Configuration → Open → Closed).
    /// </summary>
    private async Task<Cycle> CreateCycleInApplicationsClosedAsync(string department = "Biblioteca")
    {
        var cycle = Cycle.Create(
            "2024-2", department, _startDate, _endDate,
            _applicationDeadline, _interviewDate, _selectionDate, 10, "admin@test.com");
        cycle.MarkRenewalProcessCompleted();
        _dbContext.Cycles.Add(cycle);

        var location = CycleLocation.Create(cycle.Id, Guid.NewGuid(), 3, "admin@test.com");
        _dbContext.CycleLocations.Add(location);
        await _dbContext.SaveChangesAsync();

        await _openHandler.Handle(new OpenApplicationsCommand(cycle.Id), CancellationToken.None);
        await _closeHandler.Handle(new CloseApplicationsCommand(cycle.Id), CancellationToken.None);

        return cycle;
    }

    // =====================================================================
    // Happy path
    // =====================================================================

    [Fact]
    public async Task Handle_WithCycleInApplicationsClosed_ReturnsSuccessWithApplicationsOpenStatus()
    {
        // Arrange
        var cycle = await CreateCycleInApplicationsClosedAsync();

        var command = new ReopenApplicationsCommand(cycle.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(CycleStatus.ApplicationsOpen);
    }

    [Fact]
    public async Task Handle_WithValidCycle_PersistsStatusChangeToDatabase()
    {
        // Arrange
        var cycle = await CreateCycleInApplicationsClosedAsync();

        var command = new ReopenApplicationsCommand(cycle.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var savedCycle = await _dbContext.Cycles.FindAsync(cycle.Id);
        savedCycle!.Status.Should().Be(CycleStatus.ApplicationsOpen);
    }

    [Fact]
    public async Task Handle_WithValidCycle_ReturnsCycleDtoWithCorrectId()
    {
        // Arrange
        var cycle = await CreateCycleInApplicationsClosedAsync();

        var command = new ReopenApplicationsCommand(cycle.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(cycle.Id);
    }

    [Fact]
    public async Task Handle_AfterReopening_AllowsClosingAgain()
    {
        // Arrange — verificar que el flujo completo Open→Close→Reopen→Close funciona
        var cycle = await CreateCycleInApplicationsClosedAsync();

        await _handler.Handle(new ReopenApplicationsCommand(cycle.Id), CancellationToken.None);

        // Act — cerrar de nuevo tras reabrir
        var closeResult = await _closeHandler.Handle(
            new CloseApplicationsCommand(cycle.Id), CancellationToken.None);

        // Assert
        closeResult.IsSuccess.Should().BeTrue();
        closeResult.Value.Status.Should().Be(CycleStatus.ApplicationsClosed);
    }

    // =====================================================================
    // Error: ciclo no encontrado
    // =====================================================================

    [Fact]
    public async Task Handle_WithNonExistentCycleId_ReturnsCycleNotFoundFailure()
    {
        // Arrange
        var command = new ReopenApplicationsCommand(Guid.NewGuid());

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
        // Arrange — ciclo en Configuration (no ApplicationsClosed)
        var cycle = Cycle.Create(
            "2024-2", "Biblioteca", _startDate, _endDate,
            _applicationDeadline, _interviewDate, _selectionDate, 10, "admin@test.com");
        cycle.MarkRenewalProcessCompleted();
        _dbContext.Cycles.Add(cycle);
        await _dbContext.SaveChangesAsync();

        var command = new ReopenApplicationsCommand(cycle.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be($"{CycleAppError.INVALID_TRANSITION}");
    }

    [Fact]
    public async Task Handle_WhenCycleIsInApplicationsOpen_ReturnsInvalidTransitionFailure()
    {
        // Arrange — ciclo en ApplicationsOpen (no ApplicationsClosed)
        var cycle = Cycle.Create(
            "2024-2", "Biblioteca", _startDate, _endDate,
            _applicationDeadline, _interviewDate, _selectionDate, 10, "admin@test.com");
        cycle.MarkRenewalProcessCompleted();
        _dbContext.Cycles.Add(cycle);

        var location = CycleLocation.Create(cycle.Id, Guid.NewGuid(), 3, "admin@test.com");
        _dbContext.CycleLocations.Add(location);
        await _dbContext.SaveChangesAsync();

        await _openHandler.Handle(new OpenApplicationsCommand(cycle.Id), CancellationToken.None);

        var command = new ReopenApplicationsCommand(cycle.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be($"{CycleAppError.INVALID_TRANSITION}");
    }
}
