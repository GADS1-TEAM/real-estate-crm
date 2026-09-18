using RealEstateCrm.Contracts.Catalogs;

namespace RealEstateCrm.BuildingBlocks.Catalogs;

/// <summary>
/// Puerto de lectura de catálogos comerciales (D7, plan Wave 2): consulta HTTP a
/// <c>platform-config-service</c> con caché corta, para que otros servicios owner (ej.
/// <c>party-service</c> en V2-PTY-001, y luego <c>pipe-service</c>) validen un <c>code</c> contra
/// el catálogo vigente sin mantener una réplica local.
/// </summary>
public interface ICatalogReaderPort
{
    /// <summary>
    /// Trae las entradas de un <c>catalogType</c> (uno de
    /// <see cref="RealEstateCrm.Contracts.Catalogs.CatalogTypes"/>) y el <c>catalogVersion</c>
    /// vigente. <paramref name="activeOnly"/> en <see langword="true"/> es lo que un consumidor
    /// de negocio normalmente quiere (D7: "no ofrecer códigos dados de baja" en una alta nueva).
    /// </summary>
    Task<CatalogQueryResultV1> GetAsync(string catalogType, bool activeOnly, CancellationToken cancellationToken = default);
}
