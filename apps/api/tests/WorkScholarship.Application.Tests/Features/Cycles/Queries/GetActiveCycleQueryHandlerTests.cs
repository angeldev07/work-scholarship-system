using FluentAssertions;
using WorkScholarship.Application.Features.Cycles.Queries.GetActiveCycle;
using WorkScholarship.Application.Tests.TestHelpers;
using WorkScholarship.Domain.Entities;
using WorkScholarship.Domain.Enums;
using WorkScholarship.Infrastructure.Data;

namespace WorkScholarship.Application.Tests.Features.Cycles.Queries;

[Trait("Category", "Application")]
[Trait("Feature", "Cycles")]
[Trait("Component", "GetActiveCycleQueryHandler")]
public class GetActiveCycleQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly GetActiveCycleQueryHandler _handler;

    private static readonly DateTime _now = DateTime.UtcNow;

    public GetActiveCycleQueryHandlerTests()
    {
        _dbContext = DbContextFactory.CreateInMemoryContext();
        _handler = new GetActiveCycleQueryHandler(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    private Cycle CreateCycle(string name, string department)
    {
        return Cycle.Create(
            name, department,
            _now.AddDays(30), _now.AddDays(180),
            _now.AddDays(40), _now.AddDays(50), _now.AddDays(60),
            10, "admin@test.com");
    }

    // =====================================================================
    // Retorna null cuando no hay ciclo activo
    // =====================================================================

    [Fact]
    public async Task Handle_WithNoCyclesForDepartment_ReturnsSuccessWithNullValue()
    {
        // Arrange
        var query = new GetActiveCycleQuery("Biblioteca");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithCyclesForOtherDepartmentOnly_ReturnsNullValue()
    {
        // Arrange
        var otherDeptCycle = CreateCycle("2024-1", "Informatica");
        _dbContext.Cycles.Add(otherDeptCycle);
        await _dbContext.SaveChangesAsync();

        var query = new GetActiveCycleQuery("Biblioteca");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    // =====================================================================
    // Retorna el ciclo cuando existe
    // =====================================================================

    [Fact]
    public async Task Handle_WithActiveCycleForDepartment_ReturnsCycleDto()
    {
        // Arrange
        var cycle = CreateCycle("2024-2", "Biblioteca");
        _dbContext.Cycles.Add(cycle);
        await _dbContext.SaveChangesAsync();

        var query = new GetActiveCycleQuery("Biblioteca");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("2024-2");
        result.Value.Department.Should().Be("Biblioteca");
        result.Value.Status.Should().Be(CycleStatus.Configuration);
    }

    [Fact]
    public async Task Handle_WithCycleInConfigurationStatus_ReturnsThatCycle()
    {
        // Arrange — cualquier estado distinto a Closed se considera "activo"
        var cycle = CreateCycle("2024-1", "Biblioteca"); // Starts in Configuration
        _dbContext.Cycles.Add(cycle);
        await _dbContext.SaveChangesAsync();

        var query = new GetActiveCycleQuery("Biblioteca");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be(CycleStatus.Configuration);
    }

    // =====================================================================
    // Búsqueda case-insensitive por departamento
    // =====================================================================

    [Fact]
    public async Task Handle_WithDepartmentInDifferentCase_FindsCycleCorrectly()
    {
        // Arrange
        var cycle = CreateCycle("2024-1", "Biblioteca");
        _dbContext.Cycles.Add(cycle);
        await _dbContext.SaveChangesAsync();

        var query = new GetActiveCycleQuery("BIBLIOTECA");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.Department.Should().Be("Biblioteca");
    }

    // =====================================================================
    // Contadores en el DTO
    // =====================================================================

    [Fact]
    public async Task Handle_WithCycleHavingLocationsAndSupervisors_ReturnsCorrectCounts()
    {
        // Arrange
        var cycle = CreateCycle("2024-1", "Biblioteca");
        _dbContext.Cycles.Add(cycle);

        var cycleLocation = CycleLocation.Create(cycle.Id, Guid.NewGuid(), 3, "admin@test.com");
        _dbContext.CycleLocations.Add(cycleLocation);

        var assignment = SupervisorAssignment.Create(cycle.Id, cycleLocation.Id, Guid.NewGuid(), "admin@test.com");
        _dbContext.SupervisorAssignments.Add(assignment);

        await _dbContext.SaveChangesAsync();

        var query = new GetActiveCycleQuery("Biblioteca");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value!.LocationsCount.Should().Be(1);
        result.Value.SupervisorsCount.Should().Be(1);
    }
}
