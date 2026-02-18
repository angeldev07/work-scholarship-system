namespace WorkScholarship.Domain.Common;

/// <summary>
/// Clase base para todas las entidades del dominio.
/// Proporciona propiedades comunes como identificador, auditoría y eventos de dominio.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Identificador único de la entidad.
    /// </summary>
    public Guid Id { get; protected set; } = Guid.NewGuid();

    /// <summary>
    /// Fecha y hora de creación de la entidad en UTC.
    /// </summary>
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha y hora de la última actualización de la entidad en UTC.
    /// </summary>
    public DateTime? UpdatedAt { get; protected set; }

    /// <summary>
    /// Identificador del usuario que creó la entidad.
    /// </summary>
    public string CreatedBy { get; protected set; } = string.Empty;

    /// <summary>
    /// Identificador del usuario que realizó la última actualización.
    /// </summary>
    public string? UpdatedBy { get; protected set; }

    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Colección de solo lectura de eventos de dominio asociados a esta entidad.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Agrega un evento de dominio a la colección de eventos de la entidad.
    /// </summary>
    /// <param name="domainEvent">El evento de dominio a agregar.</param>
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Remueve un evento de dominio específico de la colección.
    /// </summary>
    /// <param name="domainEvent">El evento de dominio a remover.</param>
    public void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    /// <summary>
    /// Limpia todos los eventos de dominio de la entidad.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
