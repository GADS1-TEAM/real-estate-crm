using RealEstateCrm.BuildingBlocks.Persistence;

namespace RealEstateCrm.TestSupport.Persistence;

/// <summary>
/// <see cref="IUnitOfWork"/> sin transacción real para unit tests: ejecuta <c>action</c>
/// directamente. Reutilizable por cualquier servicio.
/// </summary>
public sealed class PassthroughUnitOfWork : IUnitOfWork
{
    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default) =>
        action(cancellationToken);
}
