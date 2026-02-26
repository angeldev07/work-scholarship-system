using FluentAssertions;
using WorkScholarship.Domain.Entities;

namespace WorkScholarship.Domain.Tests.Entities;

[Trait("Category", "Domain")]
[Trait("Entity", "Location")]
public class LocationTests
{
    // =====================================================================
    // Location.Create() — Factory Method
    // =====================================================================

    [Fact]
    public void Create_WithValidParameters_ReturnsLocationWithCorrectProperties()
    {
        // Act
        var location = Location.Create(
            name: "Sala de Lectura",
            department: "Biblioteca",
            description: "Sala principal de lectura con capacidad para 50 personas",
            address: "Edificio Central, Planta Baja",
            imageUrl: "https://example.com/sala-lectura.jpg",
            createdBy: "admin@test.com");

        // Assert
        location.Should().NotBeNull();
        location.Name.Should().Be("Sala de Lectura");
        location.Department.Should().Be("Biblioteca");
        location.Description.Should().Be("Sala principal de lectura con capacidad para 50 personas");
        location.Address.Should().Be("Edificio Central, Planta Baja");
        location.ImageUrl.Should().Be("https://example.com/sala-lectura.jpg");
        location.IsActive.Should().BeTrue();
        location.CreatedBy.Should().Be("admin@test.com");
        location.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WithNullOptionalFields_ReturnsLocationWithNullProperties()
    {
        // Act
        var location = Location.Create(
            name: "Sala de Referencia",
            department: "Biblioteca",
            description: null,
            address: null,
            imageUrl: null,
            createdBy: "admin@test.com");

        // Assert
        location.Description.Should().BeNull();
        location.Address.Should().BeNull();
        location.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyStringOptionalFields_ReturnsLocationWithNullProperties()
    {
        // Act — strings vacíos deben tratarse como null
        var location = Location.Create(
            name: "Sala de Revistas",
            department: "Biblioteca",
            description: "   ",
            address: "",
            imageUrl: "  ",
            createdBy: "admin@test.com");

        // Assert
        location.Description.Should().BeNull();
        location.Address.Should().BeNull();
        location.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void Create_TrimsWhitespaceFromNameAndDepartment()
    {
        // Act
        var location = Location.Create(
            "  Sala de Cómputo  ", "  Centro de Cómputo  ",
            null, null, null, "admin@test.com");

        // Assert
        location.Name.Should().Be("Sala de Cómputo");
        location.Department.Should().Be("Centro de Cómputo");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyName_ThrowsArgumentException(string? name)
    {
        // Act
        var act = () => Location.Create(name!, "Biblioteca", null, null, null, "admin@test.com");

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
        var act = () => Location.Create("Sala de Lectura", department!, null, null, null, "admin@test.com");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*departamento*");
    }

    [Fact]
    public void Create_LocationIsActiveByDefault()
    {
        // Act
        var location = Location.Create("Sala de Lectura", "Biblioteca", null, null, null, "admin@test.com");

        // Assert
        location.IsActive.Should().BeTrue();
    }

    // =====================================================================
    // Location.Deactivate() — Desactivar ubicación
    // =====================================================================

    [Fact]
    public void Deactivate_ActiveLocation_SetsIsActiveToFalse()
    {
        // Arrange
        var location = Location.Create("Sala de Lectura", "Biblioteca", null, null, null, "admin@test.com");

        // Act
        location.Deactivate("admin@test.com");

        // Assert
        location.IsActive.Should().BeFalse();
        location.UpdatedBy.Should().Be("admin@test.com");
        location.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Deactivate_AlreadyInactiveLocation_SetsIsActiveToFalse()
    {
        // Arrange
        var location = Location.Create("Sala de Lectura", "Biblioteca", null, null, null, "admin@test.com");
        location.Deactivate("admin@test.com");

        // Act — idempotente, no lanza excepción
        location.Deactivate("admin@test.com");

        // Assert
        location.IsActive.Should().BeFalse();
    }

    // =====================================================================
    // Location.Activate() — Reactivar ubicación
    // =====================================================================

    [Fact]
    public void Activate_InactiveLocation_SetsIsActiveToTrue()
    {
        // Arrange
        var location = Location.Create("Sala de Lectura", "Biblioteca", null, null, null, "admin@test.com");
        location.Deactivate("admin@test.com");

        // Act
        location.Activate("admin@test.com");

        // Assert
        location.IsActive.Should().BeTrue();
        location.UpdatedBy.Should().Be("admin@test.com");
    }

    [Fact]
    public void Activate_AlreadyActiveLocation_SetsIsActiveToTrue()
    {
        // Arrange
        var location = Location.Create("Sala de Lectura", "Biblioteca", null, null, null, "admin@test.com");

        // Act — idempotente, no lanza excepción
        location.Activate("admin@test.com");

        // Assert
        location.IsActive.Should().BeTrue();
    }

    // =====================================================================
    // Location.Update() — Actualizar datos maestros
    // =====================================================================

    [Fact]
    public void Update_WithValidParameters_UpdatesProperties()
    {
        // Arrange
        var location = Location.Create("Sala de Lectura", "Biblioteca", null, null, null, "admin@test.com");

        // Act
        location.Update(
            name: "Sala de Lectura General",
            department: "Biblioteca Central",
            description: "Nueva descripción actualizada",
            address: "Edificio A, Planta 1",
            imageUrl: "https://example.com/nueva-imagen.jpg",
            updatedBy: "admin@test.com");

        // Assert
        location.Name.Should().Be("Sala de Lectura General");
        location.Department.Should().Be("Biblioteca Central");
        location.Description.Should().Be("Nueva descripción actualizada");
        location.Address.Should().Be("Edificio A, Planta 1");
        location.ImageUrl.Should().Be("https://example.com/nueva-imagen.jpg");
        location.UpdatedBy.Should().Be("admin@test.com");
        location.UpdatedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithNullOrEmptyName_ThrowsArgumentException(string? name)
    {
        // Arrange
        var location = Location.Create("Sala de Lectura", "Biblioteca", null, null, null, "admin@test.com");

        // Act
        var act = () => location.Update(name!, "Biblioteca", null, null, null, "admin@test.com");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*nombre*");
    }
}
