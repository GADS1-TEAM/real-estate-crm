namespace RealEstateCrm.BuildingBlocks.Infrastructure.HealthChecks;

/// <summary>Tags usados para separar el check de vivo (sin dependencias) del de listo (con dependencias).</summary>
public static class CrmHealthCheckTags
{
    /// <summary>Checks que <c>/health/ready</c> evalúa. <c>/health/live</c> no evalúa ningún check (ver <see cref="CrmHealthCheckEndpointRouteBuilderExtensions"/>).</summary>
    public const string Ready = "ready";
}
