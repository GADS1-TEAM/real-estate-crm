namespace RealEstateCrm.Contracts.Errors;

/// <summary>
/// Forma estable v1 de error público, compatible con RFC 7807 (Problem Details), para todas
/// las APIs V2. Independiente de ASP.NET Core: <c>contracts</c> no referencia paquetes de
/// infraestructura, así que este tipo se puede usar también del lado de eventos/consumidores.
/// </summary>
/// <param name="Type">URI que identifica el tipo de problema. "about:blank" si no hay una página específica.</param>
/// <param name="Title">Resumen corto y legible del problema.</param>
/// <param name="Status">Código HTTP asociado.</param>
/// <param name="Detail">Explicación específica de esta instancia del problema.</param>
/// <param name="Instance">URI de la request que produjo el error.</param>
/// <param name="ErrorCode">Código estable de <see cref="ErrorCodes"/> u otro catálogo de dominio.</param>
/// <param name="CorrelationId">Identificador de la operación, para correlacionar con logs/eventos.</param>
/// <param name="Errors">Errores de validación por campo, si <see cref="ErrorCode"/> es <see cref="ErrorCodes.ValidationError"/>.</param>
public sealed record ProblemDetailsV1(
    string Type,
    string Title,
    int Status,
    string? Detail,
    string? Instance,
    string ErrorCode,
    Guid CorrelationId,
    IReadOnlyDictionary<string, string[]>? Errors = null);
