namespace RealEstateCrm.BuildingBlocks.Infrastructure.Catalogs;

/// <summary>Configuración del cliente HTTP hacia <c>platform-config-service</c> (D7).</summary>
public sealed class CatalogClientOptions
{
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Duración de la caché en memoria de <see cref="HttpCatalogReaderPort"/> (mismo criterio que <c>AccessService:CacheDuration</c>, D2).</summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromSeconds(60);
}
