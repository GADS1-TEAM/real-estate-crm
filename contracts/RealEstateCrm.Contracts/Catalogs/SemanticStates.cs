namespace RealEstateCrm.Contracts.Catalogs;

/// <summary>
/// Los tres estados semánticos protegidos de <see cref="CatalogTypes.CommercialStage"/>
/// (CAT-001): el Administrador edita etiqueta, orden y activación de una etapa, nunca su
/// significado. V2-CAT-001 solo expone este dato; la regla de proceso ("LOST exige
/// <c>lossReasonCode</c>", "OPEN no pasa a WON/LOST") la aplica <c>pipe-service</c>
/// (V2-PIPE-001), no este servicio.
/// </summary>
public static class SemanticStates
{
    public const string Open = "OPEN";
    public const string Won = "WON";
    public const string Lost = "LOST";

    public static readonly IReadOnlyCollection<string> All = new[] { Open, Won, Lost };

    public static bool IsValid(string semanticState) => All.Contains(semanticState, StringComparer.Ordinal);
}
