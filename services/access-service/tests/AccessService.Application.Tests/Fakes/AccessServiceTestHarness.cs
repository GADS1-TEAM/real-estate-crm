using AccessService.Application.Users;
using AccessService.Domain;
using RealEstateCrm.TestSupport.Messaging;
using RealEstateCrm.TestSupport.Persistence;

namespace AccessService.Application.Tests.Fakes;

/// <summary>Wiring en memoria de <see cref="UserAccountService"/> compartido por los tests de Application.</summary>
public sealed class AccessServiceTestHarness
{
    public InMemoryRepository<UserAccount, Guid> UserAccounts { get; } = new(u => u.UserId);

    public InMemoryRepository<RoleAssignment, Guid> RoleAssignments { get; } = new(r => r.UserId);

    public InMemoryOutbox Outbox { get; } = new();

    public FakeUserAccountReadPort UserAccountReads { get; }

    public UserAccountService Service { get; }

    public AccessServiceTestHarness()
    {
        UserAccountReads = new FakeUserAccountReadPort(UserAccounts);
        Service = new UserAccountService(
            UserAccounts,
            RoleAssignments,
            UserAccountReads,
            new PassthroughUnitOfWork(),
            Outbox,
            TimeProvider.System);
    }
}
