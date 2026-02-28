using FluentAssertions;
using NSubstitute;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Cycles.Queries.GetCycleById;
using WorkScholarship.Application.Tests.TestHelpers;
using WorkScholarship.Domain.Entities;
using WorkScholarship.Domain.Enums;
using WorkScholarship.Infrastructure.Data;

namespace WorkScholarship.Application.Tests.Features.Cycles.Queries;

[Trait("Category", "Application")]
[Trait("Feature", "Cycles")]
[Trait("Component", "GetCycleByIdQueryHandler")]
public class GetCycleByIdQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly GetCycleByIdQueryHandler _handler;

    private static readonly DateTime _now = DateTime.UtcNow;
    private static readonly DateTime _startDate = _now.AddDays(30);
    private static readonly DateTime _endDate = _now.AddDays(180);

    public GetCycleByIdQueryHandlerTests()
    {
        _dbContext = DbContextFactory.CreateInMemoryContext();
        _handler = new GetCycleByIdQueryHandler(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    private Cycle CreateCycle(string name = "2024-2", string department = "Biblioteca")
    {
        return Cycle.Create(
            name, department, _startDate, _endDate,
            _now.AddDays(40), _now.AddDays(50), _now.AddDays(60), 10, "admin@test.com");
    }

    // =====================================================================
    // Happy path
    // =====================================================================

    [Fact]
    public async Task Handle_WithExistingCycleId_ReturnsSuccessWithCycleDetailDto()
    {
        // Arrange
        var cycle = CreateCycle();
        _dbContext.Cycles.Add(cycle);
        await _dbContext.SaveChangesAsync();

        var query = new GetCycleByIdQuery(cycle.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(cycle.Id);
        result.Value.Name.Should().Be("2024-2");
        result.Value.Department.Should().Be("Biblioteca");
        result.Value.Status.Should().Be(CycleStatus.Configuration);
    }

    [Fact]
    public async Task Handle_WithExistingCycle_ReturnsCorrectCounts()
    {
        // Arrange
        var cycle = CreateCycle();
        _dbContext.Cycles.Add(cycle);

        var locationId = Guid.NewGuid();
        var cycleLocation = CycleLocation.Create(cycle.Id, locationId, 3, "admin@test.com");
        _dbContext.CycleLocations.Add(cycleLocation);

        var supervisorId = Guid.NewGuid();
        var assignment = SupervisorAssignment.Create(cycle.Id, cycleLocation.Id, supervisorId, "admin@test.com");
        _dbContext.SupervisorAssignments.Add(assignment);

        await _dbContext.SaveChangesAsync();

        var query = new GetCycleByIdQuery(cycle.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.LocationsCount.Should().Be(1);
        result.Value.SupervisorsCount.Should().Be(1);
        result.Value.ScholarsCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithCycleHavingInactiveLocations_CountsOnlyActiveLocations()
    {
        // Arrange
        var cycle = CreateCycle();
        _dbContext.Cycles.Add(cycle);

        var activeCL = CycleLocation.Create(cycle.Id, Guid.NewGuid(), 3, "admin@test.com");
        _dbContext.CycleLocations.Add(activeCL);

        var inactiveCL = CycleLocation.Create(cycle.Id, Guid.NewGuid(), 2, "admin@test.com");
        inactiveCL.Deactivate("admin@test.com");
        _dbContext.CycleLocations.Add(inactiveCL);

        await _dbContext.SaveChangesAsync();

        var query = new GetCycleByIdQuery(cycle.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.LocationsCount.Should().Be(1); // Solo la activa
    }

    // =====================================================================
    // Error path
    // =====================================================================

    [Fact]
    public async Task Handle_WithNonExistentCycleId_ReturnsCycleNotFoundFailure()
    {
        // Arrange
        var query = new GetCycleByIdQuery(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be($"{CycleAppError.CYCLE_NOT_FOUND}");
    }

    // =====================================================================
    // Mapeo de campos
    // =====================================================================

    [Fact]
    public async Task Handle_WithExistingCycle_MapsAllDatesCorrectly()
    {
        // Arrange
        var cycle = CreateCycle();
        _dbContext.Cycles.Add(cycle);
        await _dbContext.SaveChangesAsync();

        var query = new GetCycleByIdQuery(cycle.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.StartDate.Should().Be(_startDate);
        result.Value.EndDate.Should().Be(_endDate);
        result.Value.ClosedAt.Should().BeNull();
        result.Value.ClosedBy.Should().BeNull();
        result.Value.CreatedBy.Should().Be("admin@test.com");
    }
}
