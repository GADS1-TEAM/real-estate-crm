using AccessService.Application.Ports;
using AccessService.Domain;
using RealEstateCrm.BuildingBlocks.Authorization;
using RealEstateCrm.BuildingBlocks.Persistence;
using RealEstateCrm.Contracts.Authorization;

namespace AccessService.Application.Authorization;

/// <summary>
/// Implementación local (en memoria/Mongo directo, sin HTTP) de <see cref="IAuthorizationPort"/>
/// para access-service. Es la fuente de la verdad de la matriz rol→permiso: la usa tanto el
/// propio access-service (para autorizar sus comandos) como el endpoint
/// <c>POST /api/v1/authorization/evaluate</c> que consumen los demás servicios a través del
/// adapter HTTP (<c>HttpAuthorizationPort</c> en <c>BuildingBlocks.Infrastructure</c>).
/// </summary>
/// <remarks>
/// access-service NO se registra a sí mismo con <c>HttpAuthorizationPort</c>: sería un loop HTTP
/// contra sí mismo. Se registra con esta clase (D2, plan Wave 2 sección 5).
/// <para>
/// Orden de evaluación (pedido explícito): PENDING → <see cref="DenyReasons.UserPending"/>;
/// INACTIVE → <see cref="DenyReasons.UserInactive"/>; rol sin el permiso →
/// <see cref="DenyReasons.PermissionNotGranted"/>. Este orden importa para el mensaje que ve el
/// actor: un usuario PENDING nunca ve "no tenés el permiso", ve "todavía no te habilitaron".
/// </para>
/// </remarks>
public sealed class AccessServiceAuthorizationEvaluator(
    IUserAccountReadPort userAccountReads,
    IRepository<RoleAssignment, Guid> roleAssignments) : IAuthorizationPort
{
    public async Task<AuthorizationDecision> EvaluateAsync(
        Guid actorId,
        string permission,
        string resourceType,
        Guid? resourceId,
        CancellationToken cancellationToken = default)
    {
        var user = await userAccountReads.GetByKeycloakSubjectAsync(actorId, cancellationToken);

        if (user is null || user.Status == UserStatus.Pending)
        {
            return AuthorizationDecision.Deny(DenyReasons.UserPending);
        }

        if (user.Status == UserStatus.Inactive)
        {
            return AuthorizationDecision.Deny(DenyReasons.UserInactive);
        }

        var roleAssignment = await roleAssignments.GetByIdAsync(user.UserId, cancellationToken);

        if (roleAssignment is null || !PermissionMatrix.RoleHasPermission(roleAssignment.RoleCode, permission))
        {
            return AuthorizationDecision.Deny(DenyReasons.PermissionNotGranted);
        }

        return AuthorizationDecision.Allow();
    }
}
