namespace RealEstateCrm.Contracts.Catalogs;

/// <summary>
/// Respuesta de <c>GET /api/v1/catalogs/{catalogType}</c>: las entradas solicitadas más el
/// <see cref="CatalogVersion"/> vigente de ese <c>catalogType</c> (decisión del equipo,
/// V2-CAT-001: versión por entrada + contador de publicación por <c>catalogType</c>, sin
/// snapshot inmutable). Un consumidor (ej. <c>party-service</c>, y luego <c>pipe-service</c>)
/// guarda <c>code + CatalogVersion</c> junto con el registro que lo usó.
/// </summary>
/// <param name="CatalogVersion">
/// Contador incremental por <c>catalogType</c>, aumentado únicamente por
/// <c>PublishCatalogVersion</c>. Permite saber EN QUÉ MOMENTO de publicación se escribió un
/// <c>code</c>; no reconstruye el contenido completo del catálogo en esa versión (no hay
/// snapshot histórico en V2-CAT-001).
/// </param>
public sealed record CatalogQueryResultV1(IReadOnlyList<CatalogEntryV1> Entries, int CatalogVersion);
