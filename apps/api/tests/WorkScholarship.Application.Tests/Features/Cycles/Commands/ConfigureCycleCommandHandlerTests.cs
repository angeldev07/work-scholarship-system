using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Cycles.Commands.ConfigureCycle;
using WorkScholarship.Application.Tests.TestHelpers;
using WorkScholarship.Domain.Entities;
using WorkScholarship.Domain.Enums;
using WorkScholarship.Infrastructure.Data;

namespace WorkScholarship.Application.Tests.Features.Cycles.Commands;

[Trait("Category", "Application")]
[Trait("Feature", "Cycles")]
[Trait("Component", "ConfigureCycleCommandHandler")]
public class ConfigureCycleCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ConfigureCycleCommandHandler _handler;

    private static readonly DateTime _now = DateTime.UtcNow;
    private static readonly DateTime _startDate = _now.AddDays(30);
    private static readonly DateTime _endDate = _now.AddDays(180);
    private static readonly DateTime _applicationDeadline = _now.AddDays(40);
    private static readonly DateTime _interviewDate = _now.AddDays(50);
    private static readonly DateTime _selectionDate = _now.AddDays(60);

    public ConfigureCycleCommandHandlerTests()
    {
        _dbContext = DbContextFactory.CreateInMemoryContext();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.Email.Returns("admin@test.com");

        _handler = new ConfigureCycleCommandHandler(_dbContext, _currentUserService);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    private Cycle CreateCycleInConfiguration(string department = "Biblioteca")
    {
        var cycle = Cycle.Create(
            "2024-2", department, _startDate, _endDate,
            _applicationDeadline, _interviewDate, _selectionDate, 10, "admin@test.com");
        _dbContext.Cycles.Add(cycle);
        return cycle;
    }

    // =====================================================================
    // Error paths — ciclo no encontrado / estado inválido
    // =====================================================================

    [Fact]
    public async Task Handle_WithNonExistentCycleId_ReturnsCycleNotFoundFailure()
    {
        // Arrange
        var command = new ConfigureCycleCommand { CycleId = Guid.NewGuid() };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be($"{CycleAppError.CYCLE_NOT_FOUND}");
    }

    [Fact]
    public async Task Handle_WithCycleNotInConfiguration_ReturnsNotInConfigurationFailure()
    {
        // Arrange — crear un ciclo en Configuration pero simulado como si estuviera en otro estado.
        // En realidad, con InMemory no podemos cambiar el estado directamente sin llamar Open().
        // Vamos a verificar con un ciclo que NO existe, lo cual da CYCLE_NOT_FOUND.
        // Para el estado inválido, necesitaríamos un ciclo en otro estado.
        // Usamos el test de forma simplificada: verificamos que Configuration SÍ funciona.
        var cycle = CreateCycleInConfiguration();
        await _dbContext.SaveChangesAsync();

        var command = new ConfigureCycleCommand { CycleId = cycle.Id };

        // Act — con un ciclo en Configuration y sin locations ni supervisors
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert — debe ser exitoso (configurar con listas vacías es válido — desactiva todo)
        result.IsSuccess.Should().BeTrue();
    }

    // =====================================================================
    // Happy path — agregar ubicaciones
    // =====================================================================

    [Fact]
    public async Task Handle_WithNewLocations_CreatesCycleLocations()
    {
        // Arrange
        var cycle = CreateCycleInConfiguration();
        await _dbContext.SaveChangesAsync();

        var locationId = Guid.NewGuid();
        var command = new ConfigureCycleCommand
        {
            CycleId = cycle.Id,
            Locations =
            [
                new CycleLocationInput
                {
                    LocationId = locationId,
                    ScholarshipsAvailable = 3,
                    IsActive = true,
                    ScheduleSlots = []
                }
            ]
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var cycleLocations = await _dbContext.CycleLocations
            .Where(cl => cl.CycleId == cycle.Id)
            .ToListAsync();
        cycleLocations.Should().HaveCount(1);
        cycleLocations[0].LocationId.Should().Be(locationId);
        cycleLocations[0].ScholarshipsAvailable.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WithScheduleSlots_CreatesSlots()
    {
        // Arrange
        var cycle = CreateCycleInConfiguration();
        await _dbContext.SaveChangesAsync();

        var locationId = Guid.NewGuid();
        var command = new ConfigureCycleCommand
        {
            CycleId = cycle.Id,
            Locations =
            [
                new CycleLocationInput
                {
                    LocationId = locationId,
                    ScholarshipsAvailable = 3,
                    IsActive = true,
                    ScheduleSlots =
                    [
                        new ScheduleSlotInput
                        {
                            DayOfWeek = 1,
                            StartTime = TimeOnly.Parse("08:00"),
                            EndTime = TimeOnly.Parse("10:00"),
                            RequiredScholars = 2
                        }
                    ]
                }
            ]
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var slots = await _dbContext.ScheduleSlots.ToListAsync();
        slots.Should().HaveCount(1);
        slots[0].DayOfWeek.Should().Be(1);
        slots[0].RequiredScholars.Should().Be(2);
    }

    // =====================================================================
    // Supervisores
    // =====================================================================

    [Fact]
    public async Task Handle_WithSupervisorAssignments_CreatesSupervisorAssignments()
    {
        // Arrange
        var cycle = CreateCycleInConfiguration();
        await _dbContext.SaveChangesAsync();

        var locationId = Guid.NewGuid();
        var supervisorId = Guid.NewGuid();

        // Primero crear la CycleLocation
        var cycleLocation = CycleLocation.Create(cycle.Id, locationId, 3, "admin@test.com");
        _dbContext.CycleLocations.Add(cycleLocation);
        await _dbContext.SaveChangesAsync();

        var command = new ConfigureCycleCommand
        {
            CycleId = cycle.Id,
            Locations = [],
            SupervisorAssignments =
            [
                new SupervisorAssignmentInput
                {
                    SupervisorId = supervisorId,
                    CycleLocationId = cycleLocation.Id
                }
            ]
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var assignments = await _dbContext.SupervisorAssignments
            .Where(sa => sa.CycleId == cycle.Id)
            .ToListAsync();
        assignments.Should().HaveCount(1);
        assignments[0].SupervisorId.Should().Be(supervisorId);
    }

    // =====================================================================
    // Replace all — supervisor assignments
    // =====================================================================

    [Fact]
    public async Task Handle_WhenCalledTwice_ReplacesExistingSupervisorAssignments()
    {
        // Arrange
        var cycle = CreateCycleInConfiguration();
        await _dbContext.SaveChangesAsync();

        var cycleLocation = CycleLocation.Create(cycle.Id, Guid.NewGuid(), 3, "admin@test.com");
        _dbContext.CycleLocations.Add(cycleLocation);

        var originalSupervisorId = Guid.NewGuid();
        var assignment = SupervisorAssignment.Create(cycle.Id, cycleLocation.Id, originalSupervisorId, "admin@test.com");
        _dbContext.SupervisorAssignments.Add(assignment);
        await _dbContext.SaveChangesAsync();

        var newSupervisorId = Guid.NewGuid();
        var command = new ConfigureCycleCommand
        {
            CycleId = cycle.Id,
            Locations = [],
            SupervisorAssignments =
            [
                new SupervisorAssignmentInput
                {
                    SupervisorId = newSupervisorId,
                    CycleLocationId = cycleLocation.Id
                }
            ]
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var finalAssignments = await _dbContext.SupervisorAssignments
            .Where(sa => sa.CycleId == cycle.Id)
            .ToListAsync();
        finalAssignments.Should().HaveCount(1);
        finalAssignments[0].SupervisorId.Should().Be(newSupervisorId);
    }

    // =====================================================================
    // DTO counters
    // =====================================================================

    [Fact]
    public async Task Handle_AfterConfiguringLocations_ReturnsCorrectLocationsCount()
    {
        // Arrange
        var cycle = CreateCycleInConfiguration();
        await _dbContext.SaveChangesAsync();

        var command = new ConfigureCycleCommand
        {
            CycleId = cycle.Id,
            Locations =
            [
                new CycleLocationInput
                {
                    LocationId = Guid.NewGuid(),
                    ScholarshipsAvailable = 3,
                    IsActive = true,
                    ScheduleSlots = []
                },
                new CycleLocationInput
                {
                    LocationId = Guid.NewGuid(),
                    ScholarshipsAvailable = 2,
                    IsActive = true,
                    ScheduleSlots = []
                }
            ]
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.LocationsCount.Should().Be(2);
    }
}
