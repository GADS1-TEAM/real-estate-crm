namespace PartyService.Application;

/// <summary>
/// Error de negocio de party-service, con su código de dominio (D11) y el status HTTP al que lo
/// mapea el manejador centralizado de <c>PartyService.Api</c>.
/// </summary>
public sealed class PartyDomainException(string errorCode, int httpStatus, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;

    public int HttpStatus { get; } = httpStatus;
}

/// <summary>
/// Catálogo de errores de dominio de party-service (D11: snake_case, prefijo del dominio). Una
/// acción prohibida usa el código genérico <c>forbidden</c> (D4) con el motivo en el mensaje.
/// </summary>
public static class PartyErrorCodes
{
    public const string PartyNotFound = "party_not_found";
    public const string PartyKindMismatch = "party_kind_mismatch";
    public const string InvalidOrigin = "party_invalid_origin";
    public const string InvalidCommercialStatus = "party_invalid_commercial_status";
    public const string InvalidRelationshipType = "party_invalid_relationship_type";
    public const string RelationshipAlreadyExists = "party_relationship_already_exists";
    public const string CommercialStatusUnchanged = "party_commercial_status_unchanged";
    public const string ResponsibleUnchanged = "party_responsible_unchanged";
    public const string ResponsibleInvalid = "party_responsible_invalid";

    /// <summary>Motivo (mensaje) de un 403 <c>forbidden</c> cuando el actor no es el responsable del registro (D2).</summary>
    public const string NotResponsibleReason = "party_not_responsible";
}
