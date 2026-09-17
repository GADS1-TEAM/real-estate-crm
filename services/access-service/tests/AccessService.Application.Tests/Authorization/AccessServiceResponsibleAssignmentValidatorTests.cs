using AccessService.Application.Authorization;
using AccessService.Application.Tests.Fakes;
using AccessService.Domain;
using RealEstateCrm.Contracts.Authorization;
using RealEstateCrm.TestSupport.Persistence;

namespace AccessService.Application.Tests.Authorization;

public class AccessServiceResponsibleAssignmentValidatorTests
{
    private static (AccessServiceResponsibleAssignmentValidator Validator, InMemoryRepository<UserAccount, Guid> Users, InMemoryRepository<RoleAssignment, Guid> Roles) NewValidator()
    {
        var users = new InMemoryRepository<UserAccount, Guid>(u => u.UserId);
        var roles = new InMemoryRepository<RoleAssignment, Guid>(r => r.UserId);
        var reads = new FakeUserAccountReadPort(users);
        var authorizationPort = new AccessServiceAuthorizationEvaluator(reads, roles);
        return (new AccessServiceResponsibleAssignmentValidator(authorizationPort, users), users, roles);
    }

    [Fact]
    public async Task Unknown_resource_type_is_denied()
    {
        var (validator, _, _) = NewValidator();

        var decision = await validator.ValidateAsync(Guid.NewGuid(), "unknown-resource", Guid.NewGuid(), Guid.NewGuid());

        Assert.False(decision.Allowed);
        Assert.Equal(ResponsibleAssignmentDenyReasons.PermissionNotGranted, decision.ReasonCode);
    }

    [Fact]
    public async Task Actor_without_the_permission_is_denied_before_checking_the_target_user()
    {
        var (validator, users, roles) = NewValidator();
        var vendedor = UserAccount.CreateActive(Guid.NewGuid(), "Vendedor", "v@crm-dev.local");
        await users.AddAsync(vendedor);
        await roles.AddAsync(RoleAssignment.Create(vendedor.UserId, RoleCodes.Vendedor, vendedor.UserId, DateTimeOffset.UtcNow));

        // resourceId de un usuario que ni siquiera existe: si esto se evaluara, fallaría por otro motivo.
        var decision = await validator.ValidateAsync(vendedor.KeycloakSubject, ResourceTypes.Party, Guid.NewGuid(), Guid.NewGuid());

        Assert.False(decision.Allowed);
        Assert.Equal(DenyReasons.PermissionNotGranted, decision.ReasonCode);
    }

    [Fact]
    public async Task Nonexistent_responsible_user_is_denied()
    {
        var (validator, users, roles) = NewValidator();
        var admin = UserAccount.CreateActive(Guid.NewGuid(), "Admin", "a@crm-dev.local");
        await users.AddAsync(admin);
        await roles.AddAsync(RoleAssignment.Create(admin.UserId, RoleCodes.Administrador, admin.UserId, DateTimeOffset.UtcNow));

        var decision = await validator.ValidateAsync(admin.KeycloakSubject, ResourceTypes.Party, Guid.NewGuid(), Guid.NewGuid());

        Assert.False(decision.Allowed);
        Assert.Equal(ResponsibleAssignmentDenyReasons.ResponsibleUserNotFound, decision.ReasonCode);
    }

    [Fact]
    public async Task Inactive_responsible_user_is_denied()
    {
        var (validator, users, roles) = NewValidator();
        var admin = UserAccount.CreateActive(Guid.NewGuid(), "Admin", "a@crm-dev.local");
        await users.AddAsync(admin);
        await roles.AddAsync(RoleAssignment.Create(admin.UserId, RoleCodes.Administrador, admin.UserId, DateTimeOffset.UtcNow));

        var inactiveResponsible = UserAccount.CreateActive(Guid.NewGuid(), "Inactivo", "i@crm-dev.local").Deactivate();
        await users.AddAsync(inactiveResponsible);

        var decision = await validator.ValidateAsync(admin.KeycloakSubject, ResourceTypes.Party, Guid.NewGuid(), inactiveResponsible.UserId);

        Assert.False(decision.Allowed);
        Assert.Equal(ResponsibleAssignmentDenyReasons.ResponsibleUserInactive, decision.ReasonCode);
    }

    [Fact]
    public async Task Valid_assignment_is_allowed()
    {
        var (validator, users, roles) = NewValidator();
        var admin = UserAccount.CreateActive(Guid.NewGuid(), "Admin", "a@crm-dev.local");
        await users.AddAsync(admin);
        await roles.AddAsync(RoleAssignment.Create(admin.UserId, RoleCodes.Administrador, admin.UserId, DateTimeOffset.UtcNow));

        var responsible = UserAccount.CreateActive(Guid.NewGuid(), "Responsable", "r@crm-dev.local");
        await users.AddAsync(responsible);

        var decision = await validator.ValidateAsync(admin.KeycloakSubject, ResourceTypes.Party, Guid.NewGuid(), responsible.UserId);

        Assert.True(decision.Allowed);
    }
}
