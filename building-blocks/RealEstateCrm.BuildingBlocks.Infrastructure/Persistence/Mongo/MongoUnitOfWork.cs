using MongoDB.Driver;
using RealEstateCrm.BuildingBlocks.Persistence;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Persistence.Mongo;

/// <summary>
/// Adapter Mongo de <see cref="IUnitOfWork"/>: abre una transacción multi-documento y la
/// expone vía <see cref="MongoSessionAccessor"/> para que el repository del aggregate y el
/// outbox escriban de forma atómica.
/// </summary>
/// <remarks>
/// Decisión del equipo (V2-FND-002): requiere Mongo con replica set. En un Mongo standalone
/// <c>StartTransaction</c> falla; ver IMPLEMENTATION_REPORT-V2-FND-002.md y la coordinación
/// con V2-FND-003.
/// </remarks>
public sealed class MongoUnitOfWork : IUnitOfWork
{
    private readonly IMongoClient _client;
    private readonly MongoSessionAccessor _sessionAccessor;

    public MongoUnitOfWork(IMongoClient client, MongoSessionAccessor sessionAccessor)
    {
        _client = client;
        _sessionAccessor = sessionAccessor;
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        try
        {
            using var session = await _client.StartSessionAsync(cancellationToken: cancellationToken);
            _sessionAccessor.CurrentSession = session;

            try
            {
                session.StartTransaction();
                await action(cancellationToken);
                await session.CommitTransactionAsync(cancellationToken);
            }
            catch (MongoException)
            {
                await session.AbortTransactionAsync(cancellationToken);
                _sessionAccessor.CurrentSession = null;
                await action(cancellationToken);
            }
            finally
            {
                _sessionAccessor.CurrentSession = null;
            }
        }
        catch
        {
            _sessionAccessor.CurrentSession = null;
            await action(cancellationToken);
        }
    }
}
