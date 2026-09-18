namespace RealEstateCrm.Contracts.Catalogs;

/// <summary>
/// Los dos pipelines que ordena <see cref="CatalogTypes.CommercialStage"/> (CAT-001). Solo
/// aplica a entradas de ese <c>catalogType</c>; el resto de los catálogos no lo usa.
/// </summary>
public static class PipelineKinds
{
    public const string Demand = "DEMAND";
    public const string Supply = "SUPPLY";

    public static readonly IReadOnlyCollection<string> All = new[] { Demand, Supply };

    public static bool IsValid(string pipelineKind) => All.Contains(pipelineKind, StringComparer.Ordinal);
}
