using PlatformConfigService.Domain;

namespace PlatformConfigService.Application.Ports;

/// <summary>
/// Puerto de lectura específico de platform-config-service, complementario a
/// <c>IRepository{CatalogEntry, Guid}</c> (que solo resuelve por <c>EntryId</c>, su <c>_id</c> de
/// Mongo). Cubre las dos consultas que ese repositorio no ofrece: buscar por
/// <c>(catalogType, code)</c> (unicidad de negocio, CreateCatalogEntry) y listar por
/// <c>catalogType</c> (GetCatalog).
/// </summary>
public interface ICatalogEntryReadPort
{
    /// <summary>Resuelve la entrada de un <c>catalogType</c> con ese <c>code</c>, si existe (activa o no).</summary>
    Task<CatalogEntry?> GetByCodeAsync(string catalogType, string code, CancellationToken cancellationToken = default);

    /// <summary>Lista las entradas de un <c>catalogType</c>, opcionalmente solo las activas, ordenadas por <c>Order</c> (nulls al final) y luego por <c>Label</c>.</summary>
    Task<IReadOnlyList<CatalogEntry>> ListByCatalogTypeAsync(string catalogType, bool activeOnly, CancellationToken cancellationToken = default);
}
