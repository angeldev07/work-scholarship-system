using FluentAssertions;
using WorkScholarship.Application.Features.Cycles.Queries.ListCycles;
using WorkScholarship.Application.Tests.TestHelpers;
using WorkScholarship.Domain.Entities;
using WorkScholarship.Domain.Enums;
using WorkScholarship.Infrastructure.Data;

namespace WorkScholarship.Application.Tests.Features.Cycles.Queries;

[Trait("Category", "Application")]
[Trait("Feature", "Cycles")]
[Trait("Component", "ListCyclesQueryHandler")]
public class ListCyclesQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ListCyclesQueryHandler _handler;

    private static readonly DateTime _now = DateTime.UtcNow;

    public ListCyclesQueryHandlerTests()
    {
        _dbContext = DbContextFactory.CreateInMemoryContext();
        _handler = new ListCyclesQueryHandler(_dbContext);
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
    // Happy path — sin filtros
    // =====================================================================

    [Fact]
    public async Task Handle_WithNoCycles_ReturnsEmptyList()
    {
        // Arrange
        var query = new ListCyclesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithMultipleCycles_ReturnsAllCycles()
    {
        // Arrange
        _dbContext.Cycles.Add(CreateCycle("2024-1", "Biblioteca"));
        _dbContext.Cycles.Add(CreateCycle("2024-2", "Biblioteca"));
        _dbContext.Cycles.Add(CreateCycle("2024-1", "Informatica"));
        await _dbContext.SaveChangesAsync();

        var query = new ListCyclesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(3);
    }

    // =====================================================================
    // Filtros
    // =====================================================================

    [Fact]
    public async Task Handle_FilteredByDepartment_ReturnsOnlyCyclesForThatDepartment()
    {
        // Arrange
        _dbContext.Cycles.Add(CreateCycle("2024-1", "Biblioteca"));
        _dbContext.Cycles.Add(CreateCycle("2024-2", "Biblioteca"));
        _dbContext.Cycles.Add(CreateCycle("2024-1", "Informatica"));
        await _dbContext.SaveChangesAsync();

        var query = new ListCyclesQuery { Department = "Biblioteca" };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().AllSatisfy(c => c.Department.Should().Be("Biblioteca"));
    }

    [Fact]
    public async Task Handle_FilteredByDepartment_IsCaseInsensitive()
    {
        // Arrange
        _dbContext.Cycles.Add(CreateCycle("2024-1", "Biblioteca"));
        await _dbContext.SaveChangesAsync();

        var query = new ListCyclesQuery { Department = "BIBLIOTECA" };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_FilteredByStatus_ReturnsOnlyCyclesWithThatStatus()
    {
        // Arrange
        _dbContext.Cycles.Add(CreateCycle("2024-1", "Biblioteca")); // Configuration
        _dbContext.Cycles.Add(CreateCycle("2024-2", "Biblioteca")); // Configuration
        await _dbContext.SaveChangesAsync();

        var query = new ListCyclesQuery { Status = CycleStatus.Configuration };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().AllSatisfy(c => c.Status.Should().Be(CycleStatus.Configuration));
    }

    [Fact]
    public async Task Handle_FilteredByStatusActive_ReturnsEmptyListWhenNoneActive()
    {
        // Arrange
        _dbContext.Cycles.Add(CreateCycle("2024-1", "Biblioteca")); // Configuration
        await _dbContext.SaveChangesAsync();

        var query = new ListCyclesQuery { Status = CycleStatus.Active };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.TotalCount.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_FilteredByYear_ReturnsOnlyCyclesForThatYear()
    {
        // Arrange
        var cycleThisYear = Cycle.Create(
            "2026-1", "Biblioteca",
            new DateTime(2026, 6, 1), new DateTime(2026, 12, 15),
            new DateTime(2026, 6, 15), new DateTime(2026, 6, 22), new DateTime(2026, 6, 29),
            10, "admin@test.com");
        _dbContext.Cycles.Add(cycleThisYear);
        await _dbContext.SaveChangesAsync();

        var query = new ListCyclesQuery { Year = 2026 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.TotalCount.Should().Be(1);
    }

    // =====================================================================
    // Paginación
    // =====================================================================

    [Fact]
    public async Task Handle_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        for (var i = 1; i <= 15; i++)
        {
            _dbContext.Cycles.Add(CreateCycle($"2024-{i}", "Biblioteca"));
        }
        await _dbContext.SaveChangesAsync();

        var query = new ListCyclesQuery { Page = 2, PageSize = 5 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.TotalCount.Should().Be(15);
        result.Value.Items.Should().HaveCount(5);
        result.Value.Page.Should().Be(2);
        result.Value.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WithDefaultPageSize_Returns10Items()
    {
        // Arrange
        for (var i = 1; i <= 12; i++)
        {
            _dbContext.Cycles.Add(CreateCycle($"2024-{i}", "Biblioteca"));
        }
        await _dbContext.SaveChangesAsync();

        var query = new ListCyclesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Items.Should().HaveCount(10);
        result.Value.TotalCount.Should().Be(12);
        result.Value.HasNextPage.Should().BeTrue();
    }

    // =====================================================================
    // Ordenamiento
    // =====================================================================

    [Fact]
    public async Task Handle_WithMultipleCycles_ReturnsOrderedByCreatedAtDescending()
    {
        // Arrange
        var cycle1 = CreateCycle("First", "Biblioteca");
        _dbContext.Cycles.Add(cycle1);
        await _dbContext.SaveChangesAsync();

        var cycle2 = CreateCycle("Second", "Biblioteca");
        _dbContext.Cycles.Add(cycle2);
        await _dbContext.SaveChangesAsync();

        var query = new ListCyclesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert — el más reciente primero
        result.Value.Items[0].Name.Should().Be("Second");
        result.Value.Items[1].Name.Should().Be("First");
    }
}
