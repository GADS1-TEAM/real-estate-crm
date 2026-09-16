namespace RealEstateCrm.BuildingBlocks.Persistence;

/// <summary>
/// Puerto de unidad de trabajo transaccional: agrupa el cambio de negocio (vía
/// <see cref="IRepository{TAggregate, TId}"/>) y el encolado del evento correspondiente
/// (vía <see cref="Messaging.IOutbox"/>) en una única transacción atómica.
/// </summary>
/// <remarks>
/// Decisión del equipo (V2-FND-002): outbox con colección Mongo separada y transacción
/// multi-documento. El adapter real requiere un Mongo con replica set (a cargo de V2-FND-003);
/// ver IMPLEMENTATION_REPORT-V2-FND-002.md.
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>
    /// Ejecuta <paramref name="action"/> dentro de una transacción. Si <paramref name="action"/>
    /// lanza, la transacción se aborta y no queda ningún efecto (ni aggregate ni outbox).
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
}
