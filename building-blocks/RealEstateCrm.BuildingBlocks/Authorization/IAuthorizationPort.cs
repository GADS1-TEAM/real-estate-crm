using RealEstateCrm.Contracts.Authorization;

namespace RealEstateCrm.BuildingBlocks.Authorization;

/// <summary>
/// Puerto para resolver si el actor autenticado tiene un permiso de negocio.
/// </summary>
/// <remarks>
/// El adapter real (llamada HTTP a <c>access-service</c>, con caché corto) vive en
/// <c>RealEstateCrm.BuildingBlocks.Infrastructure</c> y lo agrega <c>V2-ACL-001</c>; este
/// puerto no sabe nada de HTTP ni de la matriz rol→permiso.
/// <para>
/// Este puerto solo evalúa <b>rol/estado del actor vs. permiso</b> (AUTHZ-002). No evalúa la
/// regla de <b>propiedad del registro</b> (ej. Vendedor solo edita lo que tiene asignado en
/// <c>responsibleUserId</c>): esa regla la aplica el propio servicio owner del recurso con su
/// dato, después de que este puerto conceda el permiso genérico (decisión D2, plan Wave 2
/// sección 5).
/// </para>
/// </remarks>
public interface IAuthorizationPort
{
    /// <summary>
    /// Evalúa si <paramref name="actorId"/> tiene <paramref name="permission"/> (ver
    /// <see cref="Permissions"/>) sobre un recurso de tipo <paramref name="resourceType"/> (ver
    /// <see cref="ResourceTypes"/>), opcionalmente identificado por
    /// <paramref name="resourceId"/>.
    /// </summary>
    Task<AuthorizationDecision> EvaluateAsync(
        Guid actorId,
        string permission,
        string resourceType,
        Guid? resourceId,
        CancellationToken cancellationToken = default);
}
