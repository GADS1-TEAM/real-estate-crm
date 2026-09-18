namespace PlatformConfigService.Domain;

/// <summary>
/// Aggregate root de una entrada de catálogo comercial (CAT-001..CAT-006). Cubre los seis
/// catálogos obligatorios con un único aggregate discriminado por <see cref="CatalogType"/>
/// (uno de <c>RealEstateCrm.Contracts.Catalogs.CatalogTypes</c>, validado en la Application
/// layer): <see cref="Order"/>/<see cref="PipelineKind"/>/<see cref="SemanticState"/> solo se
/// completan para <c>CommercialStage</c>; en el resto quedan <see langword="null"/>.
/// </summary>
/// <remarks>
/// Inmutable (record), mismo patrón que <c>UserAccount</c> (V2-ACL-001): cada método de mutación
/// devuelve una nueva instancia con <see cref="Version"/> incrementado, que
/// <c>VersionedMongoRepository</c> usa para concurrencia optimista. El Domain no referencia
/// <c>RealEstateCrm.Contracts</c> a propósito (pureza de dominio, cero <c>ProjectReference</c>):
/// <see cref="CatalogType"/>/<see cref="PipelineKind"/>/<see cref="SemanticState"/> quedan como
/// <see langword="string"/> planos; la Application layer los valida contra las constantes
/// compartidas de <c>contracts</c>.
/// <para>
/// No confundir <see cref="Version"/> (concurrencia optimista de ESTA entrada, incrementada en
/// cada Create/Update/Deactivate) con el <c>catalogVersion</c> de <c>PublishCatalogVersion</c>
/// (contador de publicación por <see cref="CatalogType"/>, en <c>ICatalogVersionPort</c>):
/// decisión del equipo documentada en el reporte de V2-CAT-001.
/// </para>
/// </remarks>
/// <param name="EntryId">Identificador propio de la entrada.</param>
/// <param name="CatalogType">Uno de los seis tipos de catálogo obligatorios.</param>
/// <param name="Code">Código estable; no cambia con la edición (solo <see cref="Label"/>/<see cref="Order"/> son editables).</param>
/// <param name="Label">Etiqueta visible, editable por el Administrador.</param>
/// <param name="Order">Orden dentro de su <see cref="PipelineKind"/>. Solo <c>CommercialStage</c>.</param>
/// <param name="PipelineKind">DEMAND/SUPPLY. Solo <c>CommercialStage</c>.</param>
/// <param name="SemanticState">OPEN/WON/LOST, estado semántico protegido: no editable. Solo <c>CommercialStage</c>.</param>
/// <param name="Active">Baja lógica (CAT-001: "Desactivar un código no rompe el historial").</param>
public sealed record CatalogEntry(
    Guid EntryId,
    string CatalogType,
    string Code,
    string Label,
    int? Order,
    string? PipelineKind,
    string? SemanticState,
    bool Active,
    int Version)
{
    /// <summary>Alta de una entrada (CreateCatalogEntry). Queda activa desde el alta.</summary>
    public static CatalogEntry Create(
        string catalogType,
        string code,
        string label,
        int? order,
        string? pipelineKind,
        string? semanticState) =>
        new(Guid.NewGuid(), catalogType, code, label, order, pipelineKind, semanticState, Active: true, Version: 1);

    /// <summary>
    /// Edición (UpdateCatalogEntry): solo etiqueta y orden. <see cref="CatalogType"/>,
    /// <see cref="Code"/>, <see cref="PipelineKind"/> y <see cref="SemanticState"/> no son
    /// editables (CAT-001: "el Administrador edita etiqueta, orden y activación, no su
    /// significado").
    /// </summary>
    public CatalogEntry UpdateDetails(string label, int? order) =>
        this with { Label = label, Order = order, Version = Version + 1 };

    /// <summary>Baja lógica (DeactivateCatalogEntry). No elimina ni muta ninguna otra colección.</summary>
    public CatalogEntry Deactivate()
    {
        if (!Active)
        {
            throw new InvalidOperationException("La entrada de catálogo ya está inactiva.");
        }

        return this with { Active = false, Version = Version + 1 };
    }
}
