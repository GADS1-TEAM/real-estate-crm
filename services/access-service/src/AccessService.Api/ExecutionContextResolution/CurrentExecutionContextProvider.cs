using AccessService.Application.Authorization;
using AccessService.Application.Ports;
using AccessService.Domain;
using RealEstateCrm.BuildingBlocks.Authentication;
using RealEstateCrm.BuildingBlocks.Infrastructure.Observability;
using RealEstateCrm.BuildingBlocks.Persistence;
using RealEstateCrm.Contracts.Context;

namespace AccessService.Api.ExecutionContextResolution;

/// <summary>
/// Resuelve el <see cref="ExecutionContextV1"/> de la request HTTP actual, a partir del JWT ya
/// validado (<see cref="IAuthenticationPort"/>). También hace el auto-provisioning de D3: si el
/// <c>sub</c> del token no tiene <see cref="UserAccount"/>, lo crea PENDING acá mismo, en el
/// primer punto donde access-service ve a ese usuario.
/// </summary>
public sealed class CurrentExecutionContextProvider(
    IAuthenticationPort authenticationPort,
    IUserAccountReadPort userAccountReads,
    IRepository<UserAccount, Guid> userAccounts,
    IRepository<RoleAssignment, Guid> roleAssignments,
    IHttpContextAccessor httpContextAccessor)
{
    /// <summary><see langword="null"/> si no hay un usuario autenticado en la request actual.</summary>
    public async Task<ExecutionContextV1?> GetAsync(CancellationToken cancellationToken = default)
    {
        var authenticatedUser = await authenticationPort.ResolveCurrentUserAsync(cancellationToken);

        if (authenticatedUser is null)
        {
            return null;
        }

        var userAccount = await userAccountReads.GetByKeycloakSubjectAsync(authenticatedUser.UserId, cancellationToken);

        if (userAccount is null)
        {
            userAccount = UserAccount.CreatePending(authenticatedUser.UserId, authenticatedUser.DisplayName, authenticatedUser.Email);
            await userAccounts.AddAsync(userAccount, cancellationToken);
        }

        var permissions = Array.Empty<string>() as IReadOnlyCollection<string>;

        if (userAccount.Status == UserStatus.Active)
        {
            var roleAssignment = await roleAssignments.GetByIdAsync(userAccount.UserId, cancellationToken);

            if (roleAssignment is not null)
            {
                permissions = PermissionMatrix.PermissionsForRole(roleAssignment.RoleCode);
            }
        }

        return new ExecutionContextV1(
            ActorId: authenticatedUser.UserId,
            DisplayName: authenticatedUser.DisplayName,
            Email: authenticatedUser.Email,
            Roles: authenticatedUser.Roles,
            Permissions: permissions,
            CorrelationId: ResolveCorrelationId(),
            CausationId: null);
    }

    private Guid ResolveCorrelationId()
    {
        var headerValue = httpContextAccessor.HttpContext?.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        return Guid.TryParse(headerValue, out var correlationId) ? correlationId : Guid.NewGuid();
    }
}
