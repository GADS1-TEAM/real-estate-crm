using AccessService.Domain;
using RealEstateCrm.Contracts.Authorization;

namespace AccessService.Application.Authorization;

/// <summary>
/// Matriz rol → permiso en backend (AUTHZ-002). Confirma, sin cambios, la matriz propuesta en
/// <c>IMPLEMENTATION_REPORT-V2-ACL-001a.md</c> (sección "Matriz rol → permiso").
/// </summary>
public static class PermissionMatrix
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> ByRole = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
    {
        [RoleCodes.Administrador] = new HashSet<string>(StringComparer.Ordinal)
        {
            Permissions.UsersRead,
            Permissions.UsersManage,
            Permissions.CatalogsRead,
            Permissions.CatalogsManage,
            Permissions.PartiesRead,
            Permissions.PartiesWrite,
            Permissions.PartiesChangeCommercialStatus,
            Permissions.PartiesAssignResponsible,
        },
        [RoleCodes.Vendedor] = new HashSet<string>(StringComparer.Ordinal)
        {
            Permissions.UsersRead,
            Permissions.CatalogsRead,
            Permissions.PartiesRead,
            Permissions.PartiesWrite,
        },
        [RoleCodes.ResponsableComercial] = new HashSet<string>(StringComparer.Ordinal)
        {
            Permissions.UsersRead,
            Permissions.CatalogsRead,
            Permissions.PartiesRead,
            Permissions.PartiesWrite,
            Permissions.PartiesChangeCommercialStatus,
            Permissions.PartiesAssignResponsible,
        },
    };

    /// <summary>
    /// Si el rol tiene el permiso. <paramref name="roleCode"/> desconocido (no debería ocurrir:
    /// <see cref="RoleCodes.IsValid"/> se valida al asignar) se trata como sin permisos.
    /// </summary>
    public static bool RoleHasPermission(string roleCode, string permission) =>
        ByRole.TryGetValue(roleCode, out var permissions) && permissions.Contains(permission);

    /// <summary>Set completo de permisos de un rol (GetEffectivePermissions). Vacío si el rol es desconocido.</summary>
    public static IReadOnlyCollection<string> PermissionsForRole(string roleCode) =>
        ByRole.TryGetValue(roleCode, out var permissions) ? permissions : Array.Empty<string>();

    /// <summary>
    /// Matriz completa (rol → permisos), para la screen ADM-03 "Roles y permisos" (evidencia
    /// requerida #1 de V2-ACL-001: "Matriz rol → permiso"). No hay otra copia de esta data:
    /// tanto <see cref="RoleHasPermission"/> como el endpoint que expone esta matriz leen del
    /// mismo diccionario.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyCollection<string>> AsDictionary() =>
        ByRole.ToDictionary(entry => entry.Key, entry => (IReadOnlyCollection<string>)entry.Value);
}
