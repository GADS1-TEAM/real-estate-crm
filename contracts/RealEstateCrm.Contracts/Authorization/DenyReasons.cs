namespace RealEstateCrm.Contracts.Authorization;

/// <summary>
/// Códigos estables de motivo de denegación para
/// <see cref="AuthorizationDecision.Deny(string)"/>.
/// </summary>
/// <remarks>
/// Cubre solo los motivos que resuelve <c>access-service</c> (rol/estado del actor). La regla
/// de propiedad del registro (ej. Vendedor solo edita lo que tiene asignado) no es un motivo de
/// este puerto: la evalúa el servicio owner con su propio dato (D2, plan Wave 2 sección 5), y no
/// tiene por qué reusar estos códigos.
/// </remarks>
public static class DenyReasons
{
    /// <summary>El rol del actor no tiene el permiso solicitado.</summary>
    public const string PermissionNotGranted = "permission_not_granted";

    /// <summary>El usuario existe pero está desactivado (INACTIVE).</summary>
    public const string UserInactive = "user_inactive";

    /// <summary>Primer login de un <c>sub</c> desconocido: usuario sin habilitar (D3).</summary>
    public const string UserPending = "user_pending";
}
