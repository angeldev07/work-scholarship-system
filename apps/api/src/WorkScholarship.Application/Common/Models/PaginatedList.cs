namespace WorkScholarship.Application.Common.Models;

/// <summary>
/// Representa un resultado de consulta paginado con metadatos de paginación.
/// Se utiliza en queries de listado para habilitar paginación del lado del servidor.
/// </summary>
/// <typeparam name="T">Tipo de los elementos en la lista paginada.</typeparam>
public class PaginatedList<T>
{
    /// <summary>
    /// Lista de elementos de la página actual.
    /// </summary>
    public List<T> Items { get; }

    /// <summary>
    /// Número de la página actual (base 1).
    /// </summary>
    public int Page { get; }

    /// <summary>
    /// Tamaño de página (cantidad de elementos por página).
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Total de elementos en la colección completa (sin filtro de paginación).
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Total de páginas calculado a partir de TotalCount y PageSize.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Indica si existe una página anterior.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Indica si existe una página siguiente.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Inicializa una instancia de PaginatedList con los datos de la página actual.
    /// </summary>
    /// <param name="items">Elementos de la página actual.</param>
    /// <param name="totalCount">Total de elementos en la colección completa.</param>
    /// <param name="page">Número de la página actual.</param>
    /// <param name="pageSize">Tamaño de cada página.</param>
    public PaginatedList(List<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    /// <summary>
    /// Crea una instancia de PaginatedList a partir de una colección en memoria.
    /// </summary>
    /// <param name="source">Colección completa de elementos.</param>
    /// <param name="page">Número de la página solicitada (base 1).</param>
    /// <param name="pageSize">Tamaño de cada página.</param>
    /// <returns>Instancia de PaginatedList con los elementos de la página solicitada.</returns>
    public static PaginatedList<T> Create(IEnumerable<T> source, int page, int pageSize)
    {
        var list = source.ToList();
        var totalCount = list.Count;
        var items = list
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedList<T>(items, totalCount, page, pageSize);
    }
}
