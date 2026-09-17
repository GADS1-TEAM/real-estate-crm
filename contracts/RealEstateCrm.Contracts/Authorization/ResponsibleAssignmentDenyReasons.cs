namespace RealEstateCrm.Contracts.Authorization;

/// <summary>
/// Códigos de motivo de denegación específicos de la validación de asignación de responsable
/// (<c>POST /api/v1/assignments/validate</c> en <c>access-service</c>, V2-ACL-001).
/// </summary>
/// <remarks>
/// Complementa a <see cref="DenyReasons"/> sin extenderlo: <see cref="DenyReasons"/> documenta
/// explícitamente que cubre solo rol/estado del ACTOR. Esta clase cubre el estado del usuario
/// DESTINO (<c>responsibleUserId</c> propuesto como responsable), que es un concepto distinto.
/// </remarks>
public static class ResponsibleAssignmentDenyReasons
{
    /// <summary>
    /// El actor no tiene el permiso de negocio para asignar responsable sobre ese
    /// <c>resourceType</c>. Mismo motivo/valor que <see cref="DenyReasons.PermissionNotGranted"/>.
    /// </summary>
    public const string PermissionNotGranted = DenyReasons.PermissionNotGranted;

    /// <summary>El <c>responsibleUserId</c> propuesto no existe en access-service.</summary>
    public const string ResponsibleUserNotFound = "responsible_user_not_found";

    /// <summary>El <c>responsibleUserId</c> propuesto existe pero está INACTIVE o PENDING.</summary>
    public const string ResponsibleUserInactive = "responsible_user_inactive";
}
