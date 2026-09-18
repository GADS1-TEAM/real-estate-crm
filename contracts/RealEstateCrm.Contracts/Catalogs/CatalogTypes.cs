namespace RealEstateCrm.Contracts.Catalogs;

/// <summary>
/// Los seis tipos de catálogo obligatorios de V2-CAT-001. Constantes compartidas (mismo criterio
/// que <c>Authorization.ResourceTypes</c>): tanto <c>platform-config-service</c> (owner) como sus
/// consumidores (D7: <c>party-service</c>, y luego <c>pipe-service</c>/<c>demand-service</c>/
/// <c>property-service</c>) referencian el mismo literal en vez de hardcodear el string por su
/// cuenta.
/// </summary>
public static class CatalogTypes
{
    public const string CommercialStage = "CommercialStage";
    public const string ActivityType = "ActivityType";
    public const string CommercialOrigin = "CommercialOrigin";
    public const string LossReason = "LossReason";
    public const string OperationType = "OperationType";
    public const string PropertyType = "PropertyType";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        CommercialStage, ActivityType, CommercialOrigin, LossReason, OperationType, PropertyType,
    };

    public static bool IsValid(string catalogType) => All.Contains(catalogType, StringComparer.Ordinal);
}
