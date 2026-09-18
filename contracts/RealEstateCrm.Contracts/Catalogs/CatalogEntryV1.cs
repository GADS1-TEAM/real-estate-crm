namespace RealEstateCrm.Contracts.Catalogs;

/// <summary>
/// Forma pública (v1) de una entrada de catálogo, compartida por dos consumidores: la screen
/// administrativa de <c>operations-bff</c> (V2-CAT-001) y el cliente HTTP de catálogos que usan
/// los demás servicios owner (D7, <c>ICatalogReaderPort</c>/<c>HttpCatalogReaderPort</c> en
/// <c>BuildingBlocks.Infrastructure/Catalogs</c>). Un solo contrato para ambos evita mantener dos
/// formas distintas del mismo dato.
/// </summary>
/// <param name="EntryId">Identificador propio de la entrada, estable entre ediciones.</param>
/// <param name="CatalogType">Uno de los seis tipos de catálogo (<see cref="CatalogTypes"/>).</param>
/// <param name="Code">Código estable que referencian otros aggregates (ej. <c>stageCode</c>, <c>lossReasonCode</c>). No cambia con la edición.</param>
/// <param name="Label">Etiqueta editable por el Administrador.</param>
/// <param name="Order">Orden dentro de su <paramref name="PipelineKind"/>. Solo aplica a <c>CommercialStage</c>; <see langword="null"/> en el resto.</param>
/// <param name="PipelineKind">DEMAND/SUPPLY (<see cref="PipelineKinds"/>). Solo aplica a <c>CommercialStage</c>; <see langword="null"/> en el resto.</param>
/// <param name="SemanticState">OPEN/WON/LOST (<see cref="SemanticStates"/>), estado semántico protegido. Solo aplica a <c>CommercialStage</c>; <see langword="null"/> en el resto.</param>
/// <param name="Active">Baja lógica: una entrada desactivada no es seleccionable en altas nuevas pero no rompe el historial.</param>
public sealed record CatalogEntryV1(
    Guid EntryId,
    string CatalogType,
    string Code,
    string Label,
    int? Order,
    string? PipelineKind,
    string? SemanticState,
    bool Active);
