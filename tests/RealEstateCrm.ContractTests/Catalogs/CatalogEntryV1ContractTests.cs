using System.Text.Json;
using RealEstateCrm.Contracts.Catalogs;
using RealEstateCrm.Contracts.Serialization;

namespace RealEstateCrm.ContractTests.Catalogs;

public class CatalogEntryV1ContractTests
{
    [Fact]
    public void CommercialStage_entry_round_trips_with_order_pipelineKind_and_semanticState()
    {
        var entry = new CatalogEntryV1(
            Guid.NewGuid(), CatalogTypes.CommercialStage, "RESERVA", "Reserva", Order: 6, PipelineKinds.Demand, SemanticStates.Open, Active: true);

        var json = JsonSerializer.Serialize(entry, RealEstateCrmJsonDefaults.Options);
        var roundTripped = JsonSerializer.Deserialize<CatalogEntryV1>(json, RealEstateCrmJsonDefaults.Options);

        Assert.Equal(entry, roundTripped);
        Assert.Contains("\"pipelineKind\":\"DEMAND\"", json, StringComparison.Ordinal);
        Assert.Contains("\"semanticState\":\"OPEN\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void A_non_stage_entry_omits_order_pipelineKind_and_semanticState_when_serialized()
    {
        var entry = new CatalogEntryV1(Guid.NewGuid(), CatalogTypes.LossReason, "PRECIO", "Precio", Order: null, PipelineKind: null, SemanticState: null, Active: true);

        var json = JsonSerializer.Serialize(entry, RealEstateCrmJsonDefaults.Options);

        Assert.DoesNotContain("pipelineKind", json, StringComparison.Ordinal);
        Assert.DoesNotContain("semanticState", json, StringComparison.Ordinal);
    }

    [Fact]
    public void CatalogQueryResultV1_round_trips_with_the_catalogVersion()
    {
        var singleEntry = new CatalogEntryV1(Guid.NewGuid(), CatalogTypes.LossReason, "OTRO", "Otro", null, null, null, true);
        var result = new CatalogQueryResultV1(new[] { singleEntry }, CatalogVersion: 3);

        var json = JsonSerializer.Serialize(result, RealEstateCrmJsonDefaults.Options);
        var roundTripped = JsonSerializer.Deserialize<CatalogQueryResultV1>(json, RealEstateCrmJsonDefaults.Options);

        // No Assert.Equal(result, roundTripped): CatalogQueryResultV1.Entries es
        // IReadOnlyList<T>, y el Equals generado para records compara esa propiedad por
        // referencia (mismo motivo por el que PagedResultContractTests tampoco lo hace).
        Assert.Equal(3, roundTripped!.CatalogVersion);
        Assert.Equal(singleEntry, Assert.Single(roundTripped.Entries));
        Assert.Contains("\"catalogVersion\":3", json, StringComparison.Ordinal);
    }
}
