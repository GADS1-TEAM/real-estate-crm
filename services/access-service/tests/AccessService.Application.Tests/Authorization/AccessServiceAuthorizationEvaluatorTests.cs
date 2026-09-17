using AccessService.Application.Authorization;
using AccessService.Application.Tests.Fakes;
using AccessService.Domain;
using RealEstateCrm.Contracts.Authorization;
using RealEstateCrm.TestSupport.Persistence;

namespace AccessService.Application.Tests.Authorization;

public class AccessServiceAuthorizationEvaluatorTests
{
    private static (AccessServiceAuthorizationEvaluator Evaluator, InMemoryRepository<UserAccount, Guid> Users, InMemoryRepository<RoleAssignment, Guid> Roles) NewEvaluator()
    {
        var users = new InMemoryRepository<UserAccount, Guid>(u => u.UserId);
        var roles = new InMemoryRepository<RoleAssignment, Guid>(r => r.UserId);
        var reads = new FakeUserAccountReadPort(users);
        return (new AccessServiceAuthorizationEvaluator(reads, roles), users, roles);
    }

    [Fact]
    public async Task Unknown_actor_is_denied_as_pending()
    {
        var (evaluator, _, _) = NewEvaluator();

        var decision = await evaluator.EvaluateAsync(Guid.NewGuid(), Permissions.UsersRead, ResourceTypes.User, null);

        Assert.False(decision.Allowed);
        Assert.Equal(DenyReasons.UserPending, decision.ReasonCode);
    }

    [Fact]
    public async Task Pending_actor_is_denied_even_with_a_role_assignment()
    {
        var (evaluator, users, roles) = NewEvaluator();
        var pending = UserAccount.CreatePending(Guid.NewGuid(), "Pendiente", "p@crm-dev.local");
        await users.AddAsync(pending);
        await roles.AddAsync(RoleAssignment.Create(pending.UserId, RoleCodes.Administrador, pending.UserId, DateTimeOffset.UtcNow));

        var decision = await evaluator.EvaluateAsync(pending.KeycloakSubject, Permissions.UsersManage, ResourceTypes.User, null);

        Assert.False(decision.Allowed);
        Assert.Equal(DenyReasons.UserPending, decision.ReasonCode);
    }

    [Fact]
    public async Task Inactive_actor_is_denied_as_inactive_not_as_missing_permission()
    {
        var (evaluator, users, roles) = NewEvaluator();
        var user = UserAccount.CreateActive(Guid.NewGuid(), "Desactivado", "d@crm-dev.local").Deactivate();
        await users.AddAsync(user);
        await roles.AddAsync(RoleAssignment.Create(user.UserId, RoleCodes.Administrador, user.UserId, DateTimeOffset.UtcNow));

        var decision = await evaluator.EvaluateAsync(user.KeycloakSubject, Permissions.UsersManage, ResourceTypes.User, null);

        Assert.False(decision.Allowed);
        Assert.Equal(DenyReasons.UserInactive, decision.ReasonCode);
    }

    [Fact]
    public async Task Active_actor_without_a_role_is_denied_permission_not_granted()
    {
        var (evaluator, users, _) = NewEvaluator();
        var user = UserAccount.CreateActive(Guid.NewGuid(), "Sin rol", "sr@crm-dev.local");
        await users.AddAsync(user);

        var decision = await evaluator.EvaluateAsync(user.KeycloakSubject, Permissions.UsersManage, ResourceTypes.User, null);

        Assert.False(decision.Allowed);
        Assert.Equal(DenyReasons.PermissionNotGranted, decision.ReasonCode);
    }

    [Fact]
    public async Task Active_vendedor_is_denied_users_manage_but_allowed_parties_write()
    {
        var (evaluator, users, roles) = NewEvaluator();
        var vendedor = UserAccount.CreateActive(Guid.NewGuid(), "Vendedor", "v@crm-dev.local");
        await users.AddAsync(vendedor);
        await roles.AddAsync(RoleAssignment.Create(vendedor.UserId, RoleCodes.Vendedor, vendedor.UserId, DateTimeOffset.UtcNow));

        var manageDecision = await evaluator.EvaluateAsync(vendedor.KeycloakSubject, Permissions.UsersManage, ResourceTypes.User, null);
        var writeDecision = await evaluator.EvaluateAsync(vendedor.KeycloakSubject, Permissions.PartiesWrite, ResourceTypes.Party, null);

        Assert.False(manageDecision.Allowed);
        Assert.True(writeDecision.Allowed);
    }
}
