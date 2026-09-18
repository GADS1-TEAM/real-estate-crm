namespace RealEstateCrm.Contracts.Events.PlatformConfig;

/// <summary>Payload v1 de <see cref="EventEnvelopeV1{TPayload}"/> para el evento "CatalogEntryCreated" publicado por platform-config-service.</summary>
public sealed record CatalogEntryCreatedV1(
    Guid EntryId,
    string CatalogType,
    string Code,
    string Label,
    int? Order,
    string? PipelineKind,
    string? SemanticState);

/// <summary>Payload v1 de <see cref="EventEnvelopeV1{TPayload}"/> para el evento "CatalogEntryUpdated" publicado por platform-config-service.</summary>
public sealed record CatalogEntryUpdatedV1(Guid EntryId, string Label, int? Order);

/// <summary>Payload v1 de <see cref="EventEnvelopeV1{TPayload}"/> para el evento "CatalogEntryDeactivated" publicado por platform-config-service.</summary>
public sealed record CatalogEntryDeactivatedV1(Guid EntryId);

/// <summary>Payload v1 de <see cref="EventEnvelopeV1{TPayload}"/> para el evento "CatalogVersionPublished" publicado por platform-config-service.</summary>
/// <param name="CatalogVersion">Nuevo valor del contador incremental de ese <c>catalogType</c> (ver <see cref="RealEstateCrm.Contracts.Catalogs.CatalogQueryResultV1"/>).</param>
public sealed record CatalogVersionPublishedV1(string CatalogType, int CatalogVersion);
