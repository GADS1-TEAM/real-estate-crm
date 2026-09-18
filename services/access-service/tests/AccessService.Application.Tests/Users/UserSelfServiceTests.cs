using AccessService.Application.Tests.Fakes;
using AccessService.Application.Users;
using AccessService.Domain;
using RealEstateCrm.Contracts.Authorization;

namespace AccessService.Application.Tests.Users;

/// <summary><c>GET /api/v1/users/me</c> (V2-PTY-001): traduce el <c>sub</c> del token al usuario de negocio.</summary>
public class UserSelfServiceTests
{
    private static async Task<(AccessServiceTestHarness Harness, UserSelfService Service)> NewAsync()
    {
        var harness = new AccessServiceTestHarness();
        await Task.CompletedTask;
        return (harness, new UserSelfService(harness.UserAccountReads, harness.RoleAssignments));
    }

    [Fact]
    public async Task An_active_user_resolves_to_their_own_userId_status_and_role_not_the_subject()
    {
        var (harness, service) = await NewAsync();
        var subject = Guid.NewGuid();
        var user = UserAccount.CreateActive(subject, "Dev Vendedor", "dev.vendedor@crm-dev.local");
        await harness.UserAccounts.AddAsync(user);
        await harness.RoleAssignments.AddAsync(RoleAssignment.Create(user.UserId, RoleCodes.Vendedor, user.UserId, DateTimeOffset.UtcNow));

        var result = await service.GetSelfAsync(subject);

        Assert.True(result.IsActive);
        Assert.Equal(user.UserId, result.User!.UserId);
        Assert.NotEqual(subject, result.User.UserId);
        Assert.Equal("Active", result.User.Status);
        Assert.Equal(RoleCodes.Vendedor, result.User.RoleCode);
    }

    [Fact]
    public async Task An_active_user_without_a_role_has_a_null_role_code()
    {
        var (harness, service) = await NewAsync();
        var subject = Guid.NewGuid();
        await harness.UserAccounts.AddAsync(UserAccount.CreateActive(subject, "Sin rol", "sinrol@crm-dev.local"));

        var result = await service.GetSelfAsync(subject);

        Assert.True(result.IsActive);
        Assert.Null(result.User!.RoleCode);
    }

    [Fact]
    public async Task A_pending_user_is_denied_with_user_pending()
    {
        var (harness, service) = await NewAsync();
        var subject = Guid.NewGuid();
        await harness.UserAccounts.AddAsync(UserAccount.CreatePending(subject, "Nuevo", "nuevo@crm-dev.local"));

        var result = await service.GetSelfAsync(subject);

        Assert.False(result.IsActive);
        Assert.Equal(DenyReasons.UserPending, result.DenyReasonCode);
    }

    [Fact]
    public async Task An_inactive_user_is_denied_with_user_inactive_even_if_they_still_have_a_role()
    {
        var (harness, service) = await NewAsync();
        var subject = Guid.NewGuid();
        var user = UserAccount.CreateActive(subject, "Baja", "baja@crm-dev.local");
        await harness.UserAccounts.AddAsync(user.Deactivate());
        await harness.RoleAssignments.AddAsync(RoleAssignment.Create(user.UserId, RoleCodes.Administrador, user.UserId, DateTimeOffset.UtcNow));

        var result = await service.GetSelfAsync(subject);

        Assert.False(result.IsActive);
        Assert.Equal(DenyReasons.UserInactive, result.DenyReasonCode);
    }

    [Fact]
    public async Task An_unknown_subject_is_treated_as_pending_without_permissions()
    {
        var (_, service) = await NewAsync();

        var result = await service.GetSelfAsync(Guid.NewGuid());

        Assert.False(result.IsActive);
        Assert.Equal(DenyReasons.UserPending, result.DenyReasonCode);
    }

    [Fact]
    public async Task One_user_never_resolves_to_another_users_data()
    {
        var (harness, service) = await NewAsync();
        var mine = UserAccount.CreateActive(Guid.NewGuid(), "Yo", "yo@crm-dev.local");
        var other = UserAccount.CreateActive(Guid.NewGuid(), "Otro", "otro@crm-dev.local");
        await harness.UserAccounts.AddAsync(mine);
        await harness.UserAccounts.AddAsync(other);

        var result = await service.GetSelfAsync(mine.KeycloakSubject);

        Assert.Equal(mine.UserId, result.User!.UserId);
    }
}
