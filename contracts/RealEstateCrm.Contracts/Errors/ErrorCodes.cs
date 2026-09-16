namespace RealEstateCrm.Contracts.Errors;

/// <summary>
/// Códigos de error transversales v1, comunes a cualquier servicio o BFF.
/// </summary>
/// <remarks>
/// Catálogo acotado a lo genérico (decisión del equipo, ver IMPLEMENTATION_REPORT-V2-FND-002.md).
/// El catálogo de errores específico de cada dominio (ej. "listing_not_available") se agrega
/// en la wave correspondiente, no acá.
/// </remarks>
public static class ErrorCodes
{
    public const string ValidationError = "validation_error";
    public const string Unauthorized = "unauthorized";
    public const string Forbidden = "forbidden";
    public const string NotFound = "not_found";
    public const string Conflict = "conflict";
}
