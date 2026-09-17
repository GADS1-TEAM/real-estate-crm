using RealEstateCrm.Contracts.Authorization;

namespace AccessService.Application.Authorization;

/// <summary>
/// Qué permiso de negocio hace falta para asignar/reasignar responsable
/// (<c>IResponsibleAssignmentValidationPort</c>, ASSIGN-001/ASSIGN-002) sobre cada
/// <c>resourceType</c>. Solo <see cref="ResourceTypes.Party"/> tiene un permiso de asignación de
/// responsable definido hoy (V2-PTY-001, todavía no existe); una wave futura que necesite asignar
/// responsable sobre otro tipo de recurso agrega su entrada acá.
/// </summary>
public static class ResourceTypePermissionMap
{
    private static readonly IReadOnlyDictionary<string, string> AssignResponsiblePermissionByResourceType =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ResourceTypes.Party] = Permissions.PartiesAssignResponsible,
        };

    /// <summary><see langword="null"/> si <paramref name="resourceType"/> no tiene asignación de responsable definida.</summary>
    public static string? PermissionForAssignResponsible(string resourceType) =>
        AssignResponsiblePermissionByResourceType.GetValueOrDefault(resourceType);
}
