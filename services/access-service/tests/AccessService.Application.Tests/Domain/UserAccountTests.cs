using AccessService.Domain;

namespace AccessService.Application.Tests.Domain;

public class UserAccountTests
{
    [Fact]
    public void CreateActive_starts_at_version_1()
    {
        var user = UserAccount.CreateActive(Guid.NewGuid(), "Nombre", "n@crm-dev.local");

        Assert.Equal(1, user.Version);
        Assert.Equal(UserStatus.Active, user.Status);
    }

    [Fact]
    public void CreatePending_has_no_permissions_until_activated()
    {
        var user = UserAccount.CreatePending(Guid.NewGuid(), "Nombre", "n@crm-dev.local");

        Assert.Equal(UserStatus.Pending, user.Status);
    }

    [Fact]
    public void Deactivate_twice_throws()
    {
        var user = UserAccount.CreateActive(Guid.NewGuid(), "Nombre", "n@crm-dev.local").Deactivate();

        Assert.Throws<InvalidOperationException>(() => user.Deactivate());
    }

    [Fact]
    public void Deactivate_increments_version_and_preserves_profile_data()
    {
        var user = UserAccount.CreateActive(Guid.NewGuid(), "Nombre", "n@crm-dev.local");

        var deactivated = user.Deactivate();

        Assert.Equal(UserStatus.Inactive, deactivated.Status);
        Assert.Equal(user.Version + 1, deactivated.Version);
        Assert.Equal(user.DisplayName, deactivated.DisplayName);
        Assert.Equal(user.Email, deactivated.Email);
        Assert.Equal(user.UserId, deactivated.UserId);
    }

    [Fact]
    public void Activate_is_a_no_op_when_already_active()
    {
        var user = UserAccount.CreateActive(Guid.NewGuid(), "Nombre", "n@crm-dev.local");

        var activated = user.Activate();

        Assert.Equal(user.Version, activated.Version);
    }

    [Fact]
    public void Activate_turns_a_pending_user_active_and_increments_version()
    {
        var user = UserAccount.CreatePending(Guid.NewGuid(), "Nombre", "n@crm-dev.local");

        var activated = user.Activate();

        Assert.Equal(UserStatus.Active, activated.Status);
        Assert.Equal(user.Version + 1, activated.Version);
    }
}
