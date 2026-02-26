using WorkScholarship.Domain.Common;

namespace WorkScholarship.Domain.Entities;

/// <summary>
/// Representa una ubicación física del programa de becas trabajo (ej: "Sala de Lectura", "Centro de Cómputo").
/// Es una entidad maestra atemporal: existe independientemente de los ciclos.
/// Las ubicaciones se vinculan a ciclos específicos mediante la entidad <see cref="CycleLocation"/>.
/// </summary>
/// <remarks>
/// Diseño dual de ubicaciones:
/// - Location (esta entidad): catálogo permanente con datos maestros de la ubicación física.
/// - CycleLocation: vínculo temporal que define la participación de una ubicación en un ciclo específico,
///   con configuración particular (becas disponibles, horarios, supervisores asignados).
/// </remarks>
public class Location : BaseEntity
{
    private Location() { }

    /// <summary>
    /// Crea una nueva ubicación en el catálogo maestro.
    /// </summary>
    /// <param name="name">Nombre descriptivo de la ubicación.</param>
    /// <param name="department">Departamento o dependencia universitaria a la que pertenece la ubicación.</param>
    /// <param name="description">Descripción detallada de la ubicación y sus funciones (opcional).</param>
    /// <param name="address">Dirección o referencia de ubicación dentro del campus (opcional).</param>
    /// <param name="imageUrl">URL de la imagen representativa de la ubicación (opcional).</param>
    /// <param name="createdBy">Identificador del administrador que registra la ubicación.</param>
    /// <returns>Nueva instancia de Location activa en el catálogo.</returns>
    /// <exception cref="ArgumentException">Si name o department están vacíos o son nulos.</exception>
    public static Location Create(
        string name,
        string department,
        string? description,
        string? address,
        string? imageUrl,
        string createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre de la ubicación es requerido.", nameof(name));

        if (string.IsNullOrWhiteSpace(department))
            throw new ArgumentException("El departamento es requerido.", nameof(department));

        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("El identificador del creador es requerido.", nameof(createdBy));

        return new Location
        {
            Name = name.Trim(),
            Department = department.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim(),
            IsActive = true,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }

    // =========================================================================
    // Propiedades
    // =========================================================================

    /// <summary>
    /// Nombre descriptivo de la ubicación física (ej: "Sala de Lectura", "Sala de Referencia").
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Departamento o dependencia universitaria a la que pertenece la ubicación.
    /// </summary>
    public string Department { get; private set; } = string.Empty;

    /// <summary>
    /// Descripción detallada de la ubicación, sus funciones y características.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Dirección o referencia de ubicación dentro del campus universitario.
    /// </summary>
    public string? Address { get; private set; }

    /// <summary>
    /// URL de la imagen representativa de la ubicación para mostrar en la interfaz.
    /// </summary>
    public string? ImageUrl { get; private set; }

    /// <summary>
    /// Indica si la ubicación está activa en el catálogo y puede ser incluida en nuevos ciclos.
    /// Las ubicaciones inactivas no están disponibles para asignación en nuevos ciclos,
    /// pero sus registros históricos en CycleLocation se conservan íntegros.
    /// </summary>
    public bool IsActive { get; private set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    private readonly List<CycleLocation> _cycleLocations = [];

    /// <summary>
    /// Colección de participaciones de esta ubicación en distintos ciclos.
    /// Cada entrada define la configuración específica de la ubicación para ese ciclo.
    /// </summary>
    public IReadOnlyCollection<CycleLocation> CycleLocations => _cycleLocations.AsReadOnly();

    // =========================================================================
    // Métodos de Comportamiento
    // =========================================================================

    /// <summary>
    /// Actualiza los datos descriptivos de la ubicación.
    /// </summary>
    /// <param name="name">Nuevo nombre de la ubicación.</param>
    /// <param name="department">Nuevo departamento al que pertenece la ubicación.</param>
    /// <param name="description">Nueva descripción (opcional).</param>
    /// <param name="address">Nueva dirección o referencia de campus (opcional).</param>
    /// <param name="imageUrl">Nueva URL de imagen (opcional).</param>
    /// <param name="updatedBy">Identificador del administrador que realiza la actualización.</param>
    /// <exception cref="ArgumentException">Si name o department están vacíos o son nulos.</exception>
    public void Update(
        string name,
        string department,
        string? description,
        string? address,
        string? imageUrl,
        string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre de la ubicación es requerido.", nameof(name));

        if (string.IsNullOrWhiteSpace(department))
            throw new ArgumentException("El departamento es requerido.", nameof(department));

        Name = name.Trim();
        Department = department.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Desactiva la ubicación del catálogo maestro.
    /// La ubicación no estará disponible para nuevos ciclos, pero los registros históricos se conservan.
    /// </summary>
    /// <param name="updatedBy">Identificador del administrador que desactiva la ubicación.</param>
    public void Deactivate(string updatedBy)
    {
        IsActive = false;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reactiva la ubicación en el catálogo maestro para que pueda incluirse en nuevos ciclos.
    /// </summary>
    /// <param name="updatedBy">Identificador del administrador que reactiva la ubicación.</param>
    public void Activate(string updatedBy)
    {
        IsActive = true;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }
}
