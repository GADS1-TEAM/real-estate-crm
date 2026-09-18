namespace PlatformConfigService.Application;

/// <summary>
/// Error de negocio de platform-config-service, con su código de dominio (D11, plan Wave 2) y el
/// status HTTP al que lo mapea el manejador centralizado de excepciones de
/// <c>PlatformConfigService.Api</c>.
/// </summary>
public sealed class PlatformConfigDomainException(string errorCode, int httpStatus, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;

    public int HttpStatus { get; } = httpStatus;
}

/// <summary>Catálogo de errores de dominio de platform-config-service (D11: snake_case, prefijo del dominio).</summary>
public static class PlatformConfigErrorCodes
{
    public const string CatalogEntryNotFound = "catalog_entry_not_found";
    public const string CatalogEntryDuplicateCode = "catalog_entry_duplicate_code";
    public const string CatalogEntryAlreadyInactive = "catalog_entry_already_inactive";
    public const string InvalidCatalogType = "invalid_catalog_type";
    public const string CatalogEntryInvalidStageFields = "catalog_entry_invalid_stage_fields";
    public const string CatalogVersionMismatch = "catalog_version_mismatch";
}
