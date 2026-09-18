namespace PlatformConfigService.Application.Ports;

/// <summary>
/// Contador de publicación por <c>catalogType</c> (decisión del equipo, V2-CAT-001: "versión por
/// entrada" — ver <c>CatalogEntry.Version</c> — más este contador separado, sin snapshot
/// inmutable). <see cref="IncrementAsync"/> respeta la sesión/transacción Mongo ambiente (mismo
/// mecanismo que <c>IRepository</c>/<c>IOutbox</c>) para que <c>PublishCatalogVersion</c> quede
/// atómico junto con el evento <c>CatalogVersionPublished</c> encolado en la misma operación.
/// </summary>
public interface ICatalogVersionPort
{
    /// <summary>Versión vigente de un <c>catalogType</c>. <c>0</c> si todavía no se publicó ninguna.</summary>
    Task<int> GetCurrentAsync(string catalogType, CancellationToken cancellationToken = default);

    /// <summary>Incrementa en 1 la versión vigente de un <c>catalogType</c> (creándola en 1 si no existía) y devuelve el nuevo valor.</summary>
    Task<int> IncrementAsync(string catalogType, CancellationToken cancellationToken = default);
}
