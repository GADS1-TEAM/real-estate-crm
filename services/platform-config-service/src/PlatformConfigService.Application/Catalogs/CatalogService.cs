using PlatformConfigService.Application.Ports;
using PlatformConfigService.Domain;
using RealEstateCrm.BuildingBlocks.Messaging;
using RealEstateCrm.BuildingBlocks.Persistence;
using RealEstateCrm.Contracts.Catalogs;
using RealEstateCrm.Contracts.Context;
using RealEstateCrm.Contracts.Events;
using RealEstateCrm.Contracts.Events.PlatformConfig;

namespace PlatformConfigService.Application.Catalogs;

/// <summary>
/// Application service de platform-config-service: implementa los commands/queries de
/// V2-CAT-001 (CreateCatalogEntry, UpdateCatalogEntry, DeactivateCatalogEntry,
/// PublishCatalogVersion, GetCatalog).
/// </summary>
/// <remarks>
/// Sin capa de mediator/CQRS (plan Wave 2, sección 4, mismo criterio que
/// <c>AccessService.Application.Users.UserAccountService</c>): cada método público es, en
/// efecto, un command/query handler.
/// </remarks>
public sealed class CatalogService(
    IRepository<CatalogEntry, Guid> catalogEntries,
    ICatalogEntryReadPort catalogEntryReads,
    ICatalogVersionPort catalogVersions,
    IUnitOfWork unitOfWork,
    IOutbox outbox,
    TimeProvider timeProvider)
{
    /// <summary>
    /// CAT-001..CAT-005: alta de una entrada. Para <c>CommercialStage</c> exige
    /// <paramref name="order"/>/<paramref name="pipelineKind"/>/<paramref name="semanticState"/>;
    /// para el resto de los catálogos exige que los tres vengan vacíos (CAT-001: esos campos
    /// "solo aplican" a etapas).
    /// </summary>
    public async Task<CatalogEntryV1> CreateCatalogEntryAsync(
        ExecutionContextV1 context,
        string catalogType,
        string code,
        string label,
        int? order,
        string? pipelineKind,
        string? semanticState,
        CancellationToken cancellationToken = default)
    {
        if (!CatalogTypes.IsValid(catalogType))
        {
            throw new PlatformConfigDomainException(
                PlatformConfigErrorCodes.InvalidCatalogType,
                httpStatus: 422,
                $"'{catalogType}' no es uno de los seis catalogType obligatorios ({string.Join(", ", CatalogTypes.All)}).");
        }

        ValidateStageFields(catalogType, order, pipelineKind, semanticState);

        var existing = await catalogEntryReads.GetByCodeAsync(catalogType, code, cancellationToken);

        if (existing is not null)
        {
            throw new PlatformConfigDomainException(
                PlatformConfigErrorCodes.CatalogEntryDuplicateCode,
                httpStatus: 409,
                $"Ya existe una entrada de '{catalogType}' con code '{code}'.");
        }

        var entry = CatalogEntry.Create(catalogType, code, label, order, pipelineKind, semanticState);

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await catalogEntries.AddAsync(entry, token);
            await EnqueueEventAsync(
                context,
                "CatalogEntryCreated",
                version: 1,
                aggregateId: entry.EntryId,
                new CatalogEntryCreatedV1(entry.EntryId, entry.CatalogType, entry.Code, entry.Label, entry.Order, entry.PipelineKind, entry.SemanticState),
                token);
        }, cancellationToken);

        return ToContract(entry);
    }

    /// <summary>CAT-001..CAT-005: edita etiqueta y orden. No cambia catalogType/code/pipelineKind/semanticState (protegidos).</summary>
    public async Task<CatalogEntryV1> UpdateCatalogEntryAsync(
        ExecutionContextV1 context,
        Guid entryId,
        string label,
        int? order,
        CancellationToken cancellationToken = default)
    {
        var entry = await RequireEntryAsync(entryId, cancellationToken);
        ValidateOrderForUpdate(entry.CatalogType, order);

        var updated = entry.UpdateDetails(label, order);

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await catalogEntries.UpdateAsync(updated, token);
            await EnqueueEventAsync(
                context,
                "CatalogEntryUpdated",
                version: 1,
                aggregateId: updated.EntryId,
                new CatalogEntryUpdatedV1(updated.EntryId, updated.Label, updated.Order),
                token);
        }, cancellationToken);

        return ToContract(updated);
    }

    /// <summary>CAT-001..CAT-005: baja lógica. No elimina ni muta ninguna otra colección; nuevos registros no pueden seleccionar el code.</summary>
    public async Task<CatalogEntryV1> DeactivateCatalogEntryAsync(
        ExecutionContextV1 context,
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        var entry = await RequireEntryAsync(entryId, cancellationToken);

        CatalogEntry deactivated;
        try
        {
            deactivated = entry.Deactivate();
        }
        catch (InvalidOperationException ex)
        {
            throw new PlatformConfigDomainException(PlatformConfigErrorCodes.CatalogEntryAlreadyInactive, httpStatus: 409, ex.Message);
        }

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await catalogEntries.UpdateAsync(deactivated, token);
            await EnqueueEventAsync(
                context,
                "CatalogEntryDeactivated",
                version: 1,
                aggregateId: deactivated.EntryId,
                new CatalogEntryDeactivatedV1(deactivated.EntryId),
                token);
        }, cancellationToken);

        return ToContract(deactivated);
    }

    /// <summary>
    /// CAT-006: publica una nueva versión de un catalogType (decisión del equipo: contador
    /// incremental por catalogType, no snapshot). No muta ninguna <see cref="CatalogEntry"/>
    /// existente.
    /// </summary>
    public async Task<int> PublishCatalogVersionAsync(ExecutionContextV1 context, string catalogType, CancellationToken cancellationToken = default)
    {
        if (!CatalogTypes.IsValid(catalogType))
        {
            throw new PlatformConfigDomainException(
                PlatformConfigErrorCodes.InvalidCatalogType,
                httpStatus: 422,
                $"'{catalogType}' no es uno de los seis catalogType obligatorios ({string.Join(", ", CatalogTypes.All)}).");
        }

        var newVersion = 0;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            newVersion = await catalogVersions.IncrementAsync(catalogType, token);
            await EnqueueEventAsync(
                context,
                "CatalogVersionPublished",
                version: 1,
                aggregateId: CatalogTypeIdentity.ToAggregateId(catalogType),
                new CatalogVersionPublishedV1(catalogType, newVersion),
                token);
        }, cancellationToken);

        return newVersion;
    }

    /// <summary>
    /// GetCatalog (CAT-001..CAT-005 lectura, D7). <paramref name="expectedVersion"/> es opcional:
    /// si se pasa y no coincide con la versión vigente, devuelve 409 (decisión local: permite a
    /// un consumidor detectar que su caché quedó desactualizada sin reconstruir el contenido
    /// histórico, que V2-CAT-001 no soporta).
    /// </summary>
    public async Task<CatalogQueryResultV1> GetCatalogAsync(
        string catalogType,
        bool activeOnly,
        int? expectedVersion,
        CancellationToken cancellationToken = default)
    {
        if (!CatalogTypes.IsValid(catalogType))
        {
            throw new PlatformConfigDomainException(
                PlatformConfigErrorCodes.InvalidCatalogType,
                httpStatus: 422,
                $"'{catalogType}' no es uno de los seis catalogType obligatorios ({string.Join(", ", CatalogTypes.All)}).");
        }

        var currentVersion = await catalogVersions.GetCurrentAsync(catalogType, cancellationToken);

        if (expectedVersion is not null && expectedVersion != currentVersion)
        {
            throw new PlatformConfigDomainException(
                PlatformConfigErrorCodes.CatalogVersionMismatch,
                httpStatus: 409,
                $"La versión vigente de '{catalogType}' es {currentVersion}, no {expectedVersion}.");
        }

        var entries = await catalogEntryReads.ListByCatalogTypeAsync(catalogType, activeOnly, cancellationToken);

        return new CatalogQueryResultV1(entries.Select(ToContract).ToArray(), currentVersion);
    }

    private async Task<CatalogEntry> RequireEntryAsync(Guid entryId, CancellationToken cancellationToken) =>
        await catalogEntries.GetByIdAsync(entryId, cancellationToken)
        ?? throw new PlatformConfigDomainException(PlatformConfigErrorCodes.CatalogEntryNotFound, httpStatus: 404, $"No existe la entrada de catálogo '{entryId}'.");

    private static void ValidateStageFields(string catalogType, int? order, string? pipelineKind, string? semanticState)
    {
        if (catalogType == CatalogTypes.CommercialStage)
        {
            if (order is null || pipelineKind is null || !PipelineKinds.IsValid(pipelineKind) || semanticState is null || !SemanticStates.IsValid(semanticState))
            {
                throw new PlatformConfigDomainException(
                    PlatformConfigErrorCodes.CatalogEntryInvalidStageFields,
                    httpStatus: 422,
                    $"'{CatalogTypes.CommercialStage}' requiere order, pipelineKind (uno de {string.Join(", ", PipelineKinds.All)}) y semanticState (uno de {string.Join(", ", SemanticStates.All)}).");
            }
        }
        else if (order is not null || pipelineKind is not null || semanticState is not null)
        {
            throw new PlatformConfigDomainException(
                PlatformConfigErrorCodes.CatalogEntryInvalidStageFields,
                httpStatus: 422,
                $"order/pipelineKind/semanticState solo aplican a '{CatalogTypes.CommercialStage}'.");
        }
    }

    private static void ValidateOrderForUpdate(string catalogType, int? order)
    {
        if (catalogType == CatalogTypes.CommercialStage)
        {
            if (order is null)
            {
                throw new PlatformConfigDomainException(
                    PlatformConfigErrorCodes.CatalogEntryInvalidStageFields,
                    httpStatus: 422,
                    $"'{CatalogTypes.CommercialStage}' requiere order.");
            }
        }
        else if (order is not null)
        {
            throw new PlatformConfigDomainException(
                PlatformConfigErrorCodes.CatalogEntryInvalidStageFields,
                httpStatus: 422,
                $"order solo aplica a '{CatalogTypes.CommercialStage}'.");
        }
    }

    private Task EnqueueEventAsync<TPayload>(
        ExecutionContextV1 context,
        string name,
        int version,
        Guid aggregateId,
        TPayload payload,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        var envelope = new EventEnvelopeV1<TPayload>(
            EventId: Guid.NewGuid(),
            Name: name,
            Version: version,
            OccurredAt: now,
            ActorId: context.ActorId,
            CorrelationId: context.CorrelationId,
            CausationId: context.CausationId,
            AggregateId: aggregateId,
            Payload: payload);

        return outbox.EnqueueAsync(OutboxMessage.From(envelope, now), cancellationToken);
    }

    private static CatalogEntryV1 ToContract(CatalogEntry entry) =>
        new(entry.EntryId, entry.CatalogType, entry.Code, entry.Label, entry.Order, entry.PipelineKind, entry.SemanticState, entry.Active);
}
