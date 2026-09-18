using AccessService.Application.Ports;
using AccessService.Domain;
using RealEstateCrm.BuildingBlocks.Authorization;
using RealEstateCrm.BuildingBlocks.Persistence;
using RealEstateCrm.Contracts.Authorization;
using RealEstateCrm.Contracts.Users;

namespace AccessService.Application.Users;

/// <summary>
/// Resuelve el usuario de negocio del propio actor (<c>GET /api/v1/users/me</c>). Aditivo: no
/// toca <see cref="UserAccountService"/>. El auto-provisioning PENDING de D3 ya lo hizo
/// <c>CurrentExecutionContextProvider</c> antes de llegar acá.
/// </summary>
public sealed class UserSelfService(
    IUserAccountReadPort userAccountReads,
    IRepository<RoleAssignment, Guid> roleAssignments)
{
    /// <param name="actorSubject">El <c>sub</c> del token (<c>ExecutionContextV1.ActorId</c>), nunca un valor del request.</param>
    public async Task<UserSelfResult> GetSelfAsync(Guid actorSubject, CancellationToken cancellationToken = default)
    {
        var user = await userAccountReads.GetByKeycloakSubjectAsync(actorSubject, cancellationToken);

        if (user is null)
        {
            // No debería ocurrir: el provider ya crea el usuario PENDING. Se trata como PENDING sin permisos.
            return UserSelfResult.Denied(DenyReasons.UserPending);
        }

        if (user.Status == UserStatus.Pending)
        {
            return UserSelfResult.Denied(DenyReasons.UserPending);
        }

        if (user.Status == UserStatus.Inactive)
        {
            return UserSelfResult.Denied(DenyReasons.UserInactive);
        }

        var roleAssignment = await roleAssignments.GetByIdAsync(user.UserId, cancellationToken);

        return UserSelfResult.Active(new UserSelfV1(user.UserId, user.Status.ToString(), roleAssignment?.RoleCode));
    }
}
