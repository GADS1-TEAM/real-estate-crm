using RealEstateCrm.BuildingBlocks.Persistence;

namespace RealEstateCrm.TestSupport.Persistence;

/// <summary>
/// Implementación en memoria de <see cref="IRepository{TAggregate, TId}"/> para unit tests de
/// Application services, sin Mongo real. Reutilizable por cualquier servicio (V2-ACL-001,
/// V2-CAT-001, V2-PTY-001): mismo puerto, sin nada específico de un aggregate.
/// </summary>
public sealed class InMemoryRepository<TAggregate, TId> : IRepository<TAggregate, TId>
    where TAggregate : class
    where TId : notnull
{
    private readonly Dictionary<TId, TAggregate> _store = new();
    private readonly Func<TAggregate, TId> _idSelector;

    public InMemoryRepository(Func<TAggregate, TId> idSelector)
    {
        _idSelector = idSelector;
    }

    public Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryGetValue(id, out var aggregate) ? aggregate : null);

    public Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        _store[_idSelector(aggregate)] = aggregate;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        _store[_idSelector(aggregate)] = aggregate;
        return Task.CompletedTask;
    }

    public IReadOnlyCollection<TAggregate> All => _store.Values.ToArray();
}
