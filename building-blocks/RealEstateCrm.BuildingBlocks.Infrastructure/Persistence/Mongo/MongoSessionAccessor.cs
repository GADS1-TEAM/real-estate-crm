using MongoDB.Driver;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Persistence.Mongo;

/// <summary>
/// Lleva la sesión/transacción de Mongo ambiente de la operación actual, para que
/// <see cref="MongoRepository{TAggregate, TId}"/> y <see cref="MongoOutbox"/> escriban dentro
/// de la misma transacción que abre <see cref="MongoUnitOfWork"/>.
/// </summary>
/// <remarks>
/// Registrado con lifetime Scoped: una instancia por operación/request, nunca compartida entre
/// requests concurrentes (ver skill dotnet-thread-safety-and-shared-state).
/// </remarks>
public sealed class MongoSessionAccessor
{
    public IClientSessionHandle? CurrentSession { get; set; }
}
