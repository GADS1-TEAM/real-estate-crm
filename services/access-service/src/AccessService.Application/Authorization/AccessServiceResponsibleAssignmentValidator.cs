using AccessService.Domain;
using RealEstateCrm.BuildingBlocks.Authorization;
using RealEstateCrm.BuildingBlocks.Persistence;
using RealEstateCrm.Contracts.Authorization;

namespace AccessService.Application.Authorization;

/// <summary>
/// Implementación local de <see cref="IResponsibleAssignmentValidationPort"/> para
/// access-service: backea el endpoint <c>POST /api/v1/assignments/validate</c> que consumen los
/// servicios owner (ej. <c>party-service</c>, V2-PTY-001) a través del adapter HTTP.
/// </summary>
/// <remarks>
/// <b>Dos esquemas de id distintos en la misma llamada, a propósito:</b> <c>actorId</c> es el
/// <c>sub</c> de Keycloak del actor que está pidiendo la asignación (misma convención que
/// <c>ExecutionContextV1.ActorId</c> en todo el sistema); <c>responsibleUserId</c> es el
/// <c>userId</c> propio de access-service del usuario propuesto como responsable (el mismo id que
/// devuelve el DTO de <c>GetUsers</c>). No son intercambiables.
/// </remarks>
public sealed class AccessServiceResponsibleAssignmentValidator(
    IAuthorizationPort authorizationPort,
    IRepository<UserAccount, Guid> userAccounts) : IResponsibleAssignmentValidationPort
{
    public async Task<AuthorizationDecision> ValidateAsync(
        Guid actorId,
        string resourceType,
        Guid resourceId,
        Guid responsibleUserId,
        CancellationToken cancellationToken = default)
    {
        var requiredPermission = ResourceTypePermissionMap.PermissionForAssignResponsible(resourceType);

        if (requiredPermission is null)
        {
            return AuthorizationDecision.Deny(ResponsibleAssignmentDenyReasons.PermissionNotGranted);
        }

        var actorDecision = await authorizationPort.EvaluateAsync(actorId, requiredPermission, resourceType, resourceId, cancellationToken);

        if (!actorDecision.Allowed)
        {
            return actorDecision;
        }

        var responsibleUser = await userAccounts.GetByIdAsync(responsibleUserId, cancellationToken);

        if (responsibleUser is null)
        {
            return AuthorizationDecision.Deny(ResponsibleAssignmentDenyReasons.ResponsibleUserNotFound);
        }

        if (responsibleUser.Status != UserStatus.Active)
        {
            return AuthorizationDecision.Deny(ResponsibleAssignmentDenyReasons.ResponsibleUserInactive);
        }

        return AuthorizationDecision.Allow();
    }
}
