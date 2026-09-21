using AccessService.Application.Ports;
using AccessService.Domain;
using RealEstateCrm.Contracts.Paging;
using RealEstateCrm.TestSupport.Persistence;

namespace AccessService.Application.Tests.Fakes;

/// <summary>
/// Implementación en memoria de <see cref="IUserAccountReadPort"/> para tests de Application.
/// Lee del mismo <see cref="InMemoryRepository{TAggregate, TId}"/> que usa el test como
/// <c>IRepository{UserAccount, Guid}</c>, así que siempre ve el último estado escrito por
/// <see cref="Users.UserAccountService"/> (sin una copia separada que se desincronice).
/// </summary>
public sealed class FakeUserAccountReadPort(InMemoryRepository<UserAccount, Guid> repository) : IUserAccountReadPort
{
    public Task<UserAccount?> GetByKeycloakSubjectAsync(Guid keycloakSubject, CancellationToken cancellationToken = default) =>
        Task.FromResult(repository.All.FirstOrDefault(u => u.KeycloakSubject == keycloakSubject));

    public Task<PagedResult<UserAccount>> SearchAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var ordered = repository.All.OrderBy(u => u.DisplayName, StringComparer.Ordinal).ToList();
        var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new PagedResult<UserAccount>(items, page, pageSize, ordered.Count, page * pageSize < ordered.Count));
    }
}
