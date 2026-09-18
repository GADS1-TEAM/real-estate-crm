using System.Text.Json;
using RealEstateCrm.Contracts.Events.PlatformConfig;
using RealEstateCrm.Contracts.Serialization;

namespace RealEstateCrm.ContractTests.Events.PlatformConfig;

public class CatalogEventsV1ContractTests
{
    [Fact]
    public void CatalogEntryCreatedV1_round_trips_and_serializes_as_camelCase()
    {
        var payload = new CatalogEntryCreatedV1(Guid.NewGuid(), "CommercialStage", "RESERVA", "Reserva", 6, "DEMAND", "OPEN");

        var json = JsonSerializer.Serialize(payload, RealEstateCrmJsonDefaults.Options);
        var roundTripped = JsonSerializer.Deserialize<CatalogEntryCreatedV1>(json, RealEstateCrmJsonDefaults.Options);

        Assert.Equal(payload, roundTripped);
        Assert.Contains("\"catalogType\"", json, StringComparison.Ordinal);
        Assert.Contains("\"pipelineKind\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void CatalogEntryUpdatedV1_carries_only_label_and_order()
    {
        var entryId = Guid.NewGuid();
        var payload = new CatalogEntryUpdatedV1(entryId, "Reserva confirmada", 5);

        var json = JsonSerializer.Serialize(payload, RealEstateCrmJsonDefaults.Options);
        using var document = JsonDocument.Parse(json);

        Assert.Equal(entryId.ToString(), document.RootElement.GetProperty("entryId").GetString());
        Assert.Equal("Reserva confirmada", document.RootElement.GetProperty("label").GetString());
        Assert.Equal(5, document.RootElement.GetProperty("order").GetInt32());
    }

    [Fact]
    public void CatalogEntryDeactivatedV1_carries_only_the_entry_id()
    {
        var entryId = Guid.NewGuid();
        var payload = new CatalogEntryDeactivatedV1(entryId);

        var json = JsonSerializer.Serialize(payload, RealEstateCrmJsonDefaults.Options);
        using var document = JsonDocument.Parse(json);

        Assert.Equal(entryId.ToString(), document.RootElement.GetProperty("entryId").GetString());
    }

    [Fact]
    public void CatalogVersionPublishedV1_round_trips_the_catalogType_and_new_version()
    {
        var payload = new CatalogVersionPublishedV1("LossReason", CatalogVersion: 2);

        var json = JsonSerializer.Serialize(payload, RealEstateCrmJsonDefaults.Options);
        var roundTripped = JsonSerializer.Deserialize<CatalogVersionPublishedV1>(json, RealEstateCrmJsonDefaults.Options);

        Assert.Equal(payload, roundTripped);
    }
}
