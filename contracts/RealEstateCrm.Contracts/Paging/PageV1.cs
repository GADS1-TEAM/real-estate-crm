namespace RealEstateCrm.Contracts.Paging;

/// <summary>
/// Envelope de paginación v1 para respuestas de listado de cualquier servicio o BFF.
/// </summary>
/// <typeparam name="TItem">Tipo de DTO que se pagina.</typeparam>
/// <param name="Items">Elementos de la página actual.</param>
/// <param name="Page">Número de página, base 1.</param>
/// <param name="PageSize">Cantidad máxima de elementos por página.</param>
/// <param name="TotalCount">Cantidad total de elementos en todas las páginas.</param>
public sealed record PageV1<TItem>(
    IReadOnlyList<TItem> Items,
    int Page,
    int PageSize,
    long TotalCount)
{
    /// <summary>Cantidad total de páginas, calculada a partir de <see cref="TotalCount"/> y <see cref="PageSize"/>.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasNextPage => Page < TotalPages;

    public bool HasPreviousPage => Page > 1;
}
