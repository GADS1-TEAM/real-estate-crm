namespace AccessService.Application;

/// <summary>
/// Error de negocio de access-service, con su código de dominio (D11, plan Wave 2) y el status
/// HTTP al que lo mapea el manejador centralizado de excepciones de <c>AccessService.Api</c>.
/// </summary>
public sealed class AccessDomainException(string errorCode, int httpStatus, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;

    public int HttpStatus { get; } = httpStatus;
}

/// <summary>Catálogo de errores de dominio de access-service (D11: snake_case, prefijo del dominio).</summary>
public static class AccessErrorCodes
{
    public const string UserNotFound = "user_not_found";
    public const string UserAlreadyExists = "user_already_exists";
    public const string UserAlreadyInactive = "user_already_inactive";
    public const string InvalidRoleCode = "invalid_role_code";
}
