using FluentAssertions;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Cycles.Commands.CloseApplications;
using WorkScholarship.Application.Features.Cycles.Commands.OpenApplications;
using WorkScholarship.Application.Tests.TestHelpers;
using WorkScholarship.Domain.Entities;
using WorkScholarship.Domain.Enums;
using WorkScholarship.Infrastructure.Data;

namespace WorkScholarship.Application.Tests.Features.Cycles.Commands;

[Trait("Category", "Application")]
[Trait("Feature", "Cycles")]
[Trait("Component", "CloseApplicationsCommandHandler")]
public class CloseApplicationsCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly CloseApplicationsCommandHandler _handler;
    private readonly OpenApplicationsCommandHandler _openHandler;

    private static readonly DateTime _now = DateTime.UtcNow;
    private static readonly DateTime _startDate = _now.AddDays(30);
    private static readonly DateTime _endDate = _now.AddDays(180);
    private static readonly DateTime _applicationDeadline = _now.AddDays(40);
    private static readonly DateTime _interviewDate = _now.AddDays(50);
    private static readonly DateTime _selectionDate = _now.AddDays(60);

    public CloseApplicationsCommandHandlerTests()
    {
        _dbContext = DbContextFactory.CreateInMemoryContext();
        _handler = new CloseApplicationsCommandHandler(_dbContext);
        _openHandler = new OpenApplicationsCommandHandler(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    /// <summary>
    /// Crea un ciclo y lo deja en estado ApplicationsOpen listo para cerrar.
    /// </summary>
    private async Task<Cycle> CreateCycleInApplicationsOpenAsync(string department = "Biblioteca")
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

        return cycle;
    }

    // =====================================================================
    // Happy path
    // =====================================================================

    [Fact]
    public async Task Handle_WithCycleInApplicationsOpen_ReturnsSuccessWithApplicationsClosedStatus()
    {
        // Arrange
        var cycle = await CreateCycleInApplicationsOpenAsync();

        var command = new CloseApplicationsCommand(cycle.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(CycleStatus.ApplicationsClosed);
    }

    [Fact]
    public async Task Handle_WithValidCycle_PersistsStatusChangeToDatabase()
    {
        // Arrange
        var cycle = await CreateCycleInApplicationsOpenAsync();

        var command = new CloseApplicationsCommand(cycle.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var savedCycle = await _dbContext.Cycles.FindAsync(cycle.Id);
        savedCycle!.Status.Should().Be(CycleStatus.ApplicationsClosed);
    }

    [Fact]
    public async Task Handle_WithValidCycle_ReturnsCycleDtoWithCorrectId()
    {
        // Arrange
        var cycle = await CreateCycleInApplicationsOpenAsync();

        var command = new CloseApplicationsCommand(cycle.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(cycle.Id);
    }

    // =====================================================================
    // Error: ciclo no encontrado
    // =====================================================================

    [Fact]
    public async Task Handle_WithNonExistentCycleId_ReturnsCycleNotFoundFailure()
    {
        // Arrange
        var command = new CloseApplicationsCommand(Guid.NewGuid());

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
        // Arrange — ciclo en Configuration (no ApplicationsOpen)
        var cycle = Cycle.Create(
            "2024-2", "Biblioteca", _startDate, _endDate,
            _applicationDeadline, _interviewDate, _selectionDate, 10, "admin@test.com");
        cycle.MarkRenewalProcessCompleted();
        _dbContext.Cycles.Add(cycle);
        await _dbContext.SaveChangesAsync();

        var command = new CloseApplicationsCommand(cycle.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be($"{CycleAppError.INVALID_TRANSITION}");
    }
}
