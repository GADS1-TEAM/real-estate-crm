using AccessService.Application.Tests.Fakes;
using AccessService.Domain;
using RealEstateCrm.Contracts.Authorization;
using RealEstateCrm.Contracts.Context;

namespace AccessService.Application.Tests.Users;

public class UserAccountServiceTests
{
    private static ExecutionContextV1 ContextFor(Guid actorId) =>
        new(actorId, "Actor de test", "actor@crm-dev.local", Array.Empty<string>(), Array.Empty<string>(), Guid.NewGuid(), CausationId: null);

    private static AccessServiceTestHarness NewHarnessWithSeededAdmin(out UserAccount admin)
    {
        var harness = new AccessServiceTestHarness();
        admin = UserAccount.CreateActive(Guid.NewGuid(), "Admin de test", "admin@crm-dev.local");
        harness.UserAccounts.AddAsync(admin).GetAwaiter().GetResult();
        return harness;
    }

    [Fact]
    public async Task CreateUserAsync_persists_the_user_and_enqueues_UserCreated()
    {
        var harness = NewHarnessWithSeededAdmin(out var admin);
        var context = ContextFor(admin.KeycloakSubject);
        var newSubject = Guid.NewGuid();

        var summary = await harness.Service.CreateUserAsync(context, newSubject, "Nuevo Vendedor", "vendedor@crm-dev.local");

        Assert.Equal("Nuevo Vendedor", summary.DisplayName);
        Assert.Equal("Active", summary.Status);
        Assert.Empty(summary.RoleCodes);

        var persisted = await harness.UserAccountReads.GetByKeycloakSubjectAsync(newSubject);
        Assert.NotNull(persisted);

        var published = Assert.Single(harness.Outbox.Messages);
        Assert.Equal("UserCreated", published.Name);
        Assert.Equal(context.CorrelationId, published.CorrelationId);
    }

    [Fact]
    public async Task CreateUserAsync_rejects_a_duplicate_keycloak_subject_with_409()
    {
        var harness = NewHarnessWithSeededAdmin(out var admin);
        var context = ContextFor(admin.KeycloakSubject);
        var subject = Guid.NewGuid();
        await harness.Service.CreateUserAsync(context, subject, "Primero", "primero@crm-dev.local");

        var exception = await Assert.ThrowsAsync<AccessDomainException>(
            () => harness.Service.CreateUserAsync(context, subject, "Segundo", "segundo@crm-dev.local"));

        Assert.Equal(409, exception.HttpStatus);
        Assert.Equal(AccessErrorCodes.UserAlreadyExists, exception.ErrorCode);
    }

    [Fact]
    public async Task UpdateUserAsync_throws_404_for_an_unknown_user()
    {
        var harness = NewHarnessWithSeededAdmin(out var admin);
        var context = ContextFor(admin.KeycloakSubject);

        var exception = await Assert.ThrowsAsync<AccessDomainException>(
            () => harness.Service.UpdateUserAsync(context, Guid.NewGuid(), "X", "x@crm-dev.local"));

        Assert.Equal(404, exception.HttpStatus);
        Assert.Equal(AccessErrorCodes.UserNotFound, exception.ErrorCode);
    }

    [Fact]
    public async Task DeactivateUserAsync_marks_the_user_inactive_without_touching_other_state()
    {
        var harness = NewHarnessWithSeededAdmin(out var admin);
        var context = ContextFor(admin.KeycloakSubject);
        var created = await harness.Service.CreateUserAsync(context, Guid.NewGuid(), "A desactivar", "desactivar@crm-dev.local");

        var summary = await harness.Service.DeactivateUserAsync(context, created.UserId);

        Assert.Equal("Inactive", summary.Status);
        Assert.Equal("A desactivar", summary.DisplayName); // USR-002: no borra la historia/datos del usuario.
    }

    [Fact]
    public async Task DeactivateUserAsync_rejects_deactivating_an_already_inactive_user_with_409()
    {
        var harness = NewHarnessWithSeededAdmin(out var admin);
        var context = ContextFor(admin.KeycloakSubject);
        var created = await harness.Service.CreateUserAsync(context, Guid.NewGuid(), "Doble baja", "doble@crm-dev.local");
        await harness.Service.DeactivateUserAsync(context, created.UserId);

        var exception = await Assert.ThrowsAsync<AccessDomainException>(
            () => harness.Service.DeactivateUserAsync(context, created.UserId));

        Assert.Equal(409, exception.HttpStatus);
        Assert.Equal(AccessErrorCodes.UserAlreadyInactive, exception.ErrorCode);
    }

    [Fact]
    public async Task AssignRoleAsync_rejects_a_role_code_outside_the_three_mandatory_roles()
    {
        var harness = NewHarnessWithSeededAdmin(out var admin);
        var context = ContextFor(admin.KeycloakSubject);
        var created = await harness.Service.CreateUserAsync(context, Guid.NewGuid(), "Target", "target@crm-dev.local");

        var exception = await Assert.ThrowsAsync<AccessDomainException>(
            () => harness.Service.AssignRoleAsync(context, created.UserId, "SuperAdmin"));

        Assert.Equal(422, exception.HttpStatus);
        Assert.Equal(AccessErrorCodes.InvalidRoleCode, exception.ErrorCode);
    }

    [Fact]
    public async Task AssignRoleAsync_activates_a_pending_user_on_first_role_assignment()
    {
        var harness = NewHarnessWithSeededAdmin(out var admin);
        var context = ContextFor(admin.KeycloakSubject);

        // Simula el auto-provisioning de D3: el usuario destino llegó PENDING (no vía CreateUser).
        var pendingUser = UserAccount.CreatePending(Guid.NewGuid(), "Pendiente", "pendiente@crm-dev.local");
        await harness.UserAccounts.AddAsync(pendingUser);

        var summary = await harness.Service.AssignRoleAsync(context, pendingUser.UserId, RoleCodes.Vendedor);

        Assert.Equal("Active", summary.Status);
        Assert.Equal(new[] { RoleCodes.Vendedor }, summary.RoleCodes);
    }

    [Fact]
    public async Task AssignRoleAsync_replaces_the_previous_role_and_records_it_in_the_event()
    {
        var harness = NewHarnessWithSeededAdmin(out var admin);
        var context = ContextFor(admin.KeycloakSubject);
        var target = await harness.Service.CreateUserAsync(context, Guid.NewGuid(), "Target", "target@crm-dev.local");
        await harness.Service.AssignRoleAsync(context, target.UserId, RoleCodes.Vendedor);

        var summary = await harness.Service.AssignRoleAsync(context, target.UserId, RoleCodes.ResponsableComercial);

        Assert.Equal(new[] { RoleCodes.ResponsableComercial }, summary.RoleCodes);

        var roleAssignment = await harness.RoleAssignments.GetByIdAsync(target.UserId);
        Assert.NotNull(roleAssignment);
        Assert.Equal(RoleCodes.ResponsableComercial, roleAssignment!.RoleCode);
        Assert.Equal(2, roleAssignment.Version); // 1 = alta (Vendedor), 2 = reasignado a Responsable Comercial.

        var lastEventJson = harness.Outbox.Messages.Last(m => m.Name == "RoleAssigned").EnvelopeJson;
        Assert.Contains("\"previousRoleCode\":\"Vendedor\"", lastEventJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetEffectivePermissionsAsync_returns_empty_for_a_pending_user_even_if_it_somehow_has_a_role()
    {
        var harness = new AccessServiceTestHarness();
        var pendingUser = UserAccount.CreatePending(Guid.NewGuid(), "Pendiente", "pendiente@crm-dev.local");
        await harness.UserAccounts.AddAsync(pendingUser);

        var result = await harness.Service.GetEffectivePermissionsAsync(pendingUser.UserId);

        Assert.Empty(result.Permissions);
    }

    [Fact]
    public async Task GetEffectivePermissionsAsync_returns_the_role_matrix_for_an_active_user()
    {
        var harness = NewHarnessWithSeededAdmin(out var admin);
        var context = ContextFor(admin.KeycloakSubject);
        var target = await harness.Service.CreateUserAsync(context, Guid.NewGuid(), "Target", "target@crm-dev.local");
        await harness.Service.AssignRoleAsync(context, target.UserId, RoleCodes.Administrador);

        var result = await harness.Service.GetEffectivePermissionsAsync(target.UserId);

        Assert.Equal(RoleCodes.Administrador, result.RoleCode);
        Assert.Contains(Permissions.UsersManage, result.Permissions);
    }

    [Fact]
    public async Task GetUsersAsync_paginates_in_a_stable_order()
    {
        var harness = new AccessServiceTestHarness();
        var admin = UserAccount.CreateActive(Guid.NewGuid(), "Admin", "admin@crm-dev.local");
        await harness.UserAccounts.AddAsync(admin);
        var context = ContextFor(admin.KeycloakSubject);

        await harness.Service.CreateUserAsync(context, Guid.NewGuid(), "Beta", "beta@crm-dev.local");
        await harness.Service.CreateUserAsync(context, Guid.NewGuid(), "Alfa", "alfa@crm-dev.local");

        var page = await harness.Service.GetUsersAsync(page: 1, pageSize: 2);

        Assert.Equal(3, page.TotalCount); // admin + beta + alfa
        Assert.Equal("Admin", page.Items[0].DisplayName);
        Assert.Equal("Alfa", page.Items[1].DisplayName);
    }
}
