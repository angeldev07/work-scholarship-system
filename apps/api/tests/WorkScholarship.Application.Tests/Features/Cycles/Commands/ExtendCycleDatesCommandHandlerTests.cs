using FluentAssertions;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Cycles.Commands.CloseApplications;
using WorkScholarship.Application.Features.Cycles.Commands.CloseCycle;
using WorkScholarship.Application.Features.Cycles.Commands.ExtendDates;
using WorkScholarship.Application.Features.Cycles.Commands.OpenApplications;
using WorkScholarship.Application.Tests.TestHelpers;
using WorkScholarship.Domain.Entities;
using WorkScholarship.Domain.Enums;
using WorkScholarship.Infrastructure.Data;

namespace WorkScholarship.Application.Tests.Features.Cycles.Commands;

[Trait("Category", "Application")]
[Trait("Feature", "Cycles")]
[Trait("Component", "ExtendCycleDatesCommandHandler")]
public class ExtendCycleDatesCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ExtendCycleDatesCommandHandler _handler;
    private readonly OpenApplicationsCommandHandler _openHandler;
    private readonly CloseApplicationsCommandHandler _closeApplicationsHandler;
    private readonly CloseCycleCommandHandler _closeCycleHandler;

    private static readonly DateTime _now = DateTime.UtcNow;

    // Fechas futuras para ciclos en configuración/abiertos
    private static readonly DateTime _startDate = _now.AddDays(30);
    private static readonly DateTime _endDate = _now.AddDays(180);
    private static readonly DateTime _applicationDeadline = _now.AddDays(40);
    private static readonly DateTime _interviewDate = _now.AddDays(50);
    private static readonly DateTime _selectionDate = _now.AddDays(60);

    // Fechas pasadas para crear ciclos que puedan cerrarse
    private static readonly DateTime _pastStartDate = _now.AddDays(-180);
    private static readonly DateTime _pastEndDate = _now.AddDays(-1);
    private static readonly DateTime _pastApplicationDeadline = _now.AddDays(-160);
    private static readonly DateTime _pastInterviewDate = _now.AddDays(-150);
    private static readonly DateTime _pastSelectionDate = _now.AddDays(-140);

    public ExtendCycleDatesCommandHandlerTests()
    {
        _dbContext = DbContextFactory.CreateInMemoryContext();
        _handler = new ExtendCycleDatesCommandHandler(_dbContext);
        _openHandler = new OpenApplicationsCommandHandler(_dbContext);
        _closeApplicationsHandler = new CloseApplicationsCommandHandler(_dbContext);
        _closeCycleHandler = new CloseCycleCommandHandler(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    /// <summary>
    /// Crea un ciclo en estado Configuration listo para extender fechas.
    /// </summary>
    private async Task<Cycle> CreateCycleInConfigurationAsync(string department = "Biblioteca")
    {
        var cycle = Cycle.Create(
            "2024-2", department, _startDate, _endDate,
            _applicationDeadline, _interviewDate, _selectionDate, 10, "admin@test.com");
        cycle.MarkRenewalProcessCompleted();
        _dbContext.Cycles.Add(cycle);
        await _dbContext.SaveChangesAsync();
        return cycle;
    }

    /// <summary>
    /// Crea un ciclo y lo lleva al estado ApplicationsClosed.
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
        await _closeApplicationsHandler.Handle(new CloseApplicationsCommand(cycle.Id), CancellationToken.None);

        return cycle;
    }

    /// <summary>
    /// Crea un ciclo con fechas pasadas y lo lleva al estado Closed.
    /// Cadena completa: Configuration → Open → CloseApplications → Activate → Close.
    /// </summary>
    private async Task<Cycle> CreateCycleInClosedStateAsync(string department = "Biblioteca")
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

        await _closeCycleHandler.Handle(new CloseCycleCommand(cycle.Id), CancellationToken.None);

        return cycle;
    }

    // =====================================================================
    // Happy path
    // =====================================================================

    [Fact]
    public async Task Handle_ExtendingApplicationDeadline_ReturnsSuccessWithUpdatedDate()
    {
        // Arrange
        var cycle = await CreateCycleInConfigurationAsync();
        var newDeadline = _applicationDeadline.AddDays(5);

        var command = new ExtendCycleDatesCommand
        {
            CycleId = cycle.Id,
            NewApplicationDeadline = newDeadline
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ApplicationDeadline.Should().Be(newDeadline);
    }

    [Fact]
    public async Task Handle_ExtendingEndDate_PersistsChangeToDatabase()
    {
        // Arrange
        var cycle = await CreateCycleInConfigurationAsync();
        var newEndDate = _endDate.AddDays(30);

        var command = new ExtendCycleDatesCommand
        {
            CycleId = cycle.Id,
            NewEndDate = newEndDate
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var savedCycle = await _dbContext.Cycles.FindAsync(cycle.Id);
        savedCycle!.EndDate.Should().Be(newEndDate);
    }

    [Fact]
    public async Task Handle_ExtendingMultipleDates_UpdatesAllProvidedDates()
    {
        // Arrange
        var cycle = await CreateCycleInConfigurationAsync();
        var newApplicationDeadline = _applicationDeadline.AddDays(5);
        var newInterviewDate = _interviewDate.AddDays(10);
        var newSelectionDate = _selectionDate.AddDays(15);
        var newEndDate = _endDate.AddDays(30);

        var command = new ExtendCycleDatesCommand
        {
            CycleId = cycle.Id,
            NewApplicationDeadline = newApplicationDeadline,
            NewInterviewDate = newInterviewDate,
            NewSelectionDate = newSelectionDate,
            NewEndDate = newEndDate
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ApplicationDeadline.Should().Be(newApplicationDeadline);
        result.Value.InterviewDate.Should().Be(newInterviewDate);
        result.Value.SelectionDate.Should().Be(newSelectionDate);
        result.Value.EndDate.Should().Be(newEndDate);
    }

    // =====================================================================
    // Error: ciclo no encontrado
    // =====================================================================

    [Fact]
    public async Task Handle_WithNonExistentCycleId_ReturnsCycleNotFoundFailure()
    {
        // Arrange
        var command = new ExtendCycleDatesCommand
        {
            CycleId = Guid.NewGuid(),
            NewEndDate = _endDate.AddDays(30)
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be($"{CycleAppError.CYCLE_NOT_FOUND}");
        result.Error.Message.Should().NotBeNullOrWhiteSpace();
    }

    // =====================================================================
    // Error: ciclo cerrado (inmutable)
    // =====================================================================

    [Fact]
    public async Task Handle_WhenCycleIsClosed_ReturnsCycleClosedFailure()
    {
        // Arrange — ciclo en estado Closed (inmutable)
        var cycle = await CreateCycleInClosedStateAsync();

        var command = new ExtendCycleDatesCommand
        {
            CycleId = cycle.Id,
            NewEndDate = _pastEndDate.AddDays(30)
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be($"{CycleAppError.CYCLE_CLOSED}");
    }

    // =====================================================================
    // Error: fecha inválida (reducción de fecha)
    // =====================================================================

    [Fact]
    public async Task Handle_WithNewDateSameAsCurrentDate_ReturnsInvalidDateFailure()
    {
        // Arrange — nueva EndDate igual a la actual (no es extensión)
        var cycle = await CreateCycleInConfigurationAsync();

        var command = new ExtendCycleDatesCommand
        {
            CycleId = cycle.Id,
            NewEndDate = _endDate // igual a la fecha actual del ciclo, no mayor
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be($"{CycleAppError.INVALID_DATE}");
    }

    // =====================================================================
    // Error: transición inválida (ApplicationsClosed — fase de entrevistas)
    // =====================================================================

    [Fact]
    public async Task Handle_WhenCycleIsInApplicationsClosed_ReturnsInvalidTransitionFailure()
    {
        // Arrange — ciclo en ApplicationsClosed (fase de entrevistas)
        var cycle = await CreateCycleInApplicationsClosedAsync();

        var command = new ExtendCycleDatesCommand
        {
            CycleId = cycle.Id,
            NewEndDate = _endDate.AddDays(30)
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be($"{CycleAppError.INVALID_TRANSITION}");
    }
}
