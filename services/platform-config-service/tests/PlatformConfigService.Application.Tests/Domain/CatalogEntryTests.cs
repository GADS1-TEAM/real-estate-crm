using PlatformConfigService.Domain;

namespace PlatformConfigService.Application.Tests.Domain;

public class CatalogEntryTests
{
    [Fact]
    public void Create_starts_active_at_version_1()
    {
        var entry = CatalogEntry.Create(CatalogTypesForTests.LossReason, "PRECIO", "Precio", order: null, pipelineKind: null, semanticState: null);

        Assert.True(entry.Active);
        Assert.Equal(1, entry.Version);
    }

    [Fact]
    public void UpdateDetails_changes_label_and_order_and_increments_version()
    {
        var entry = CatalogEntry.Create(CatalogTypesForTests.CommercialStage, "RESERVA", "Reserva", order: 6, pipelineKind: "DEMAND", semanticState: "OPEN");

        var updated = entry.UpdateDetails("Reserva confirmada", 5);

        Assert.Equal("Reserva confirmada", updated.Label);
        Assert.Equal(5, updated.Order);
        Assert.Equal(entry.Version + 1, updated.Version);
        // No editable: catalogType, code, pipelineKind y semanticState no cambian con Update (CAT-001).
        Assert.Equal(entry.CatalogType, updated.CatalogType);
        Assert.Equal(entry.Code, updated.Code);
        Assert.Equal(entry.PipelineKind, updated.PipelineKind);
        Assert.Equal(entry.SemanticState, updated.SemanticState);
    }

    [Fact]
    public void Deactivate_twice_throws()
    {
        var entry = CatalogEntry.Create(CatalogTypesForTests.LossReason, "OTRO", "Otro", order: null, pipelineKind: null, semanticState: null).Deactivate();

        Assert.Throws<InvalidOperationException>(() => entry.Deactivate());
    }

    [Fact]
    public void Deactivate_does_not_touch_code_or_label_history()
    {
        var entry = CatalogEntry.Create(CatalogTypesForTests.LossReason, "PRECIO", "Precio", order: null, pipelineKind: null, semanticState: null);

        var deactivated = entry.Deactivate();

        Assert.False(deactivated.Active);
        Assert.Equal(entry.Code, deactivated.Code);
        Assert.Equal(entry.Label, deactivated.Label);
        Assert.Equal(entry.Version + 1, deactivated.Version);
    }

    /// <summary>
    /// El Domain de platform-config-service no referencia <c>RealEstateCrm.Contracts</c> (pureza,
    /// cero <c>ProjectReference</c>): estos literales evitan que el test de Domain dependa de
    /// <c>CatalogTypes</c>/<c>PipelineKinds</c>/<c>SemanticStates</c> (que sí viven en contracts).
    /// </summary>
    private static class CatalogTypesForTests
    {
        public const string CommercialStage = "CommercialStage";
        public const string LossReason = "LossReason";
    }
}
