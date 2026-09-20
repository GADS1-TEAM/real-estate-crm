namespace RealEstateCrm.Contracts.Authorization;

/// <summary>
/// Códigos de permiso v1 que <c>access-service</c> resuelve y que los servicios owner
/// verifican vía <see cref="RealEstateCrm.BuildingBlocks.Authorization.IAuthorizationPort"/>.
/// </summary>
/// <remarks>
/// Convención <c>"&lt;recurso&gt;.&lt;accion&gt;"</c> en snake_case. Este set cubre solo lo que
/// necesita Wave 2 (V2-ACL-001, V2-CAT-001, V2-PTY-001; ver
/// IMPLEMENTATION_REPORT-V2-ACL-001a.md). Waves siguientes agregan los permisos de sus propios
/// recursos acá mismo, no en otro lado: no duplicar esta convención en otro archivo.
/// </remarks>
public static class Permissions
{
    /// <summary>Listar usuarios (ej. para elegir responsable en otro recurso).</summary>
    public const string UsersRead = "users.read";

    /// <summary>Crear, editar, desactivar usuario y asignar rol.</summary>
    public const string UsersManage = "users.manage";

    /// <summary>Leer catálogos comerciales (todo usuario autenticado).</summary>
    public const string CatalogsRead = "catalogs.read";

    /// <summary>Crear, editar, desactivar y publicar versión de catálogo.</summary>
    public const string CatalogsManage = "catalogs.manage";

    /// <summary>Leer empresas, contactos y sus relaciones.</summary>
    public const string PartiesRead = "parties.read";

    /// <summary>Crear/editar empresa y contacto, y relacionarlos.</summary>
    public const string PartiesWrite = "parties.write";

    /// <summary>Cambiar el estado comercial de una party.</summary>
    public const string PartiesChangeCommercialStatus = "parties.change_commercial_status";

    /// <summary>Asignar o reasignar el responsable comercial de una party.</summary>
    public const string PartiesAssignResponsible = "parties.assign_responsible";

    public const string PropertiesRead = "properties.read";
    public const string PropertiesWrite = "properties.write";
    public const string ListingsRead = "listings.read";
    public const string ListingsWrite = "listings.write";
    public const string RequirementsRead = "requirements.read";
    public const string RequirementsWrite = "requirements.write";
    public const string CaptationsRead = "captations.read";
    public const string CaptationsWrite = "captations.write";
    public const string MatchesRead = "matches.read";
}
