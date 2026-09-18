using PlatformConfigService.Application.Tests.Fakes;
using RealEstateCrm.Contracts.Catalogs;
using RealEstateCrm.Contracts.Context;

namespace PlatformConfigService.Application.Tests.Catalogs;

public class CatalogServiceTests
{
    private static ExecutionContextV1 ContextFor(Guid actorId) =>
        new(actorId, "Administrador de test", "admin@crm-dev.local", Array.Empty<string>(), Array.Empty<string>(), Guid.NewGuid(), CausationId: null);

    [Fact]
    public async Task CreateCatalogEntryAsync_persists_a_generic_entry_and_enqueues_CatalogEntryCreated()
    {
        var harness = new CatalogServiceTestHarness();
        var context = ContextFor(Guid.NewGuid());

        var entry = await harness.Service.CreateCatalogEntryAsync(
            context, CatalogTypes.LossReason, "PRECIO", "Precio", order: null, pipelineKind: null, semanticState: null);

        Assert.Equal("Precio", entry.Label);
        Assert.True(entry.Active);

        var published = Assert.Single(harness.Outbox.Messages);
        Assert.Equal("CatalogEntryCreated", published.Name);
        Assert.Equal(context.CorrelationId, published.CorrelationId);
    }

    [Fact]
    public async Task CreateCatalogEntryAsync_persists_a_commercial_stage_with_order_pipeline_and_semantic_state()
    {
        var harness = new CatalogServiceTestHarness();
        var context = ContextFor(Guid.NewGuid());

        var entry = await harness.Service.CreateCatalogEntryAsync(
            context, CatalogTypes.CommercialStage, "RESERVA", "Reserva", order: 6, PipelineKinds.Demand, SemanticStates.Open);

        Assert.Equal(6, entry.Order);
        Assert.Equal(PipelineKinds.Demand, entry.PipelineKind);
        Assert.Equal(SemanticStates.Open, entry.SemanticState);
    }

    [Fact]
    public async Task CreateCatalogEntryAsync_rejects_an_unknown_catalogType_with_422()
    {
        var harness = new CatalogServiceTestHarness();
        var context = ContextFor(Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<PlatformConfigDomainException>(
            () => harness.Service.CreateCatalogEntryAsync(context, "NotACatalog", "X", "X", null, null, null));

        Assert.Equal(422, exception.HttpStatus);
        Assert.Equal(PlatformConfigErrorCodes.InvalidCatalogType, exception.ErrorCode);
    }

    [Fact]
    public async Task CreateCatalogEntryAsync_rejects_a_commercial_stage_missing_semantic_fields_with_422()
    {
        var harness = new CatalogServiceTestHarness();
        var context = ContextFor(Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<PlatformConfigDomainException>(
            () => harness.Service.CreateCatalogEntryAsync(context, CatalogTypes.CommercialStage, "X", "X", order: null, pipelineKind: null, semanticState: null));

        Assert.Equal(422, exception.HttpStatus);
        Assert.Equal(PlatformConfigErrorCodes.CatalogEntryInvalidStageFields, exception.ErrorCode);
    }

    [Fact]
    public async Task CreateCatalogEntryAsync_rejects_a_non_stage_catalog_with_order_populated_with_422()
    {
        var harness = new CatalogServiceTestHarness();
        var context = ContextFor(Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<PlatformConfigDomainException>(
            () => harness.Service.CreateCatalogEntryAsync(context, CatalogTypes.LossReason, "X", "X", order: 1, pipelineKind: null, semanticState: null));

        Assert.Equal(422, exception.HttpStatus);
        Assert.Equal(PlatformConfigErrorCodes.CatalogEntryInvalidStageFields, exception.ErrorCode);
    }

    [Fact]
    public async Task CreateCatalogEntryAsync_rejects_a_duplicate_code_within_the_same_catalogType_with_409()
    {
        var harness = new CatalogServiceTestHarness();
        var context = ContextFor(Guid.NewGuid());
        await harness.Service.CreateCatalogEntryAsync(context, CatalogTypes.LossReason, "PRECIO", "Precio", null, null, null);

        var exception = await Assert.ThrowsAsync<PlatformConfigDomainException>(
            () => harness.Service.CreateCatalogEntryAsync(context, CatalogTypes.LossReason, "PRECIO", "Precio (otra etiqueta)", null, null, null));

        Assert.Equal(409, exception.HttpStatus);
        Assert.Equal(PlatformConfigErrorCodes.CatalogEntryDuplicateCode, exception.ErrorCode);
    }

    [Fact]
    public async Task CreateCatalogEntryAsync_allows_the_same_code_in_two_different_catalogTypes()
    {
        var harness = new CatalogServiceTestHarness();
        var context = ContextFor(Guid.NewGuid());

        await harness.Service.CreateCatalogEntryAsync(context, CatalogTypes.LossReason, "OTRO", "Otro", null, null, null);
        var second = await harness.Service.CreateCatalogEntryAsync(context, CatalogTypes.ActivityType, "OTRO", "Otro", null, null, null);

        Assert.Equal("OTRO", second.Code);
    }

    [Fact]
    public async Task UpdateCatalogEntryAsync_changes_label_and_order_without_touching_code_or_semantic_state()
    {
        var harness = new CatalogServiceTestHarness();
        var context = ContextFor(Guid.NewGuid());
        var created = await harness.Service.CreateCatalogEntryAsync(
            context, CatalogTypes.CommercialStage, "RESERVA", "Reserva", 6, PipelineKinds.Demand, SemanticStates.Open);

        var updated = await harness.Service.UpdateCatalogEntryAsync(context, created.EntryId, "Reserva confirmada", 5);

        Assert.Equal("Reserva confirmada", updated.Label);
        Assert.Equal(5, updated.Order);
        Assert.Equal("RESERVA", updated.Code);
        Assert.Equal(SemanticStates.Open, updated.SemanticState); // OPEN/WON/LOST protegido: Update no lo toca.
    }

    [Fact]
    public async Task UpdateCatalogEntryAsync_throws_404_for_an_unknown_entry()
    {
        var harness = new CatalogServiceTestHarness();
        var context = ContextFor(Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<PlatformConfigDomainException>(
            () => harness.Service.UpdateCatalogEntryAsync(context, Guid.NewGuid(), "X", null));

        Assert.Equal(404, exception.HttpStatus);
        Assert.Equal(PlatformConfigErrorCodes.CatalogEntryNotFound, exception.ErrorCode);
    }

    [Fact]
    public async Task DeactivateCatalogEntryAsync_marks_the_entry_inactive_without_deleting_it()
    {
        var harness = new CatalogServiceTestHarness();
        var context = ContextFor(Guid.NewGuid());
        var created = await harness.Service.CreateCatalogEntryAsync(context, CatalogTypes.LossReason, "OTRO", "Otro", null, null, null);

        var deactivated = await harness.Service.DeactivateCatalogEntryAsync(context, created.EntryId);

        Assert.False(deactivated.Active);
        Assert.Equal("OTRO", deactivated.Code); // baja lógica: el código sigue existiendo para el historial (CAT-001).
    }

    [Fact]
    public async Task DeactivateCatalogEntryAsync_rejects_deactivating_twice_with_409()
    {
        var harness = new CatalogServiceTestHarness();
        var context = ContextFor(Guid.NewGuid());
        var created = await harness.Service.CreateCatalogEntryAsync(context, CatalogTypes.LossReason, "OTRO", "Otro", null, null, null);
        await harness.Service.DeactivateCatalogEntryAsync(context, created.EntryId);

        var exception = await Assert.ThrowsAsync<PlatformConfigDomainException>(
            () => harness.Service.DeactivateCatalogEntryAsync(context, created.EntryId));

        Assert.Equal(409, exception.HttpStatus);
        Assert.Equal(PlatformConfigErrorCodes.CatalogEntryAlreadyInactive, exception.ErrorCode);
    }

    [Fact]
    public async Task GetCatalogAsync_activeOnly_filters_out_deactivated_entries_but_keeps_them_in_the_full_list()
    {
        var harness = new CatalogServiceTestHarness();
        var context = ContextFor(Guid.NewGuid());
        var created = await harness.Service.CreateCatalogEntryAsync(context, CatalogTypes.LossReason, "OTRO", "Otro", null, null, null);
        await harness.Service.DeactivateCatalogEntryAsync(context, created.EntryId);

        var activeOnly = await harness.Service.GetCatalogAsync(CatalogTypes.LossReason, activeOnly: true, expectedVersion: null);
        var everything = await harness.Service.GetCatalogAsync(CatalogTypes.LossReason, activeOnly: false, expectedVersion: null);

        Assert.Empty(activeOnly.Entries);
        Assert.Single(everything.Entries);
    }

    [Fact]
    public async Task PublishCatalogVersionAsync_increments_the_counter_without_mutating_existing_entries()
    {
        var harness = new CatalogServiceTestHarness();
        var context = ContextFor(Guid.NewGuid());
        var created = await harness.Service.CreateCatalogEntryAsync(context, CatalogTypes.LossReason, "OTRO", "Otro", null, null, null);

        var firstVersion = await harness.Service.PublishCatalogVersionAsync(context, CatalogTypes.LossReason);
        var secondVersion = await harness.Service.PublishCatalogVersionAsync(context, CatalogTypes.LossReason);

        Assert.Equal(1, firstVersion);
        Assert.Equal(2, secondVersion);

        var stillThere = await harness.CatalogEntries.GetByIdAsync(created.EntryId);
        Assert.NotNull(stillThere);
        Assert.Equal(created.Label, stillThere!.Label); // publicar no muta registros existentes (CAT-006).

        var published = harness.Outbox.Messages.Where(m => m.Name == "CatalogVersionPublished").ToList();
        Assert.Equal(2, published.Count);
    }

    [Fact]
    public async Task GetCatalogAsync_reports_the_current_catalogVersion_alongside_the_entries()
    {
        var harness = new CatalogServiceTestHarness();
        var context = ContextFor(Guid.NewGuid());
        await harness.Service.PublishCatalogVersionAsync(context, CatalogTypes.LossReason);
        await harness.Service.PublishCatalogVersionAsync(context, CatalogTypes.LossReason);

        var result = await harness.Service.GetCatalogAsync(CatalogTypes.LossReason, activeOnly: false, expectedVersion: null);

        Assert.Equal(2, result.CatalogVersion);
    }

    [Fact]
    public async Task GetCatalogAsync_rejects_a_stale_expectedVersion_with_409()
    {
        var harness = new CatalogServiceTestHarness();
        var context = ContextFor(Guid.NewGuid());
        await harness.Service.PublishCatalogVersionAsync(context, CatalogTypes.LossReason);
        await harness.Service.PublishCatalogVersionAsync(context, CatalogTypes.LossReason);

        var exception = await Assert.ThrowsAsync<PlatformConfigDomainException>(
            () => harness.Service.GetCatalogAsync(CatalogTypes.LossReason, activeOnly: false, expectedVersion: 1));

        Assert.Equal(409, exception.HttpStatus);
        Assert.Equal(PlatformConfigErrorCodes.CatalogVersionMismatch, exception.ErrorCode);
    }
}
