using AccessService.Application.Ports;
using AccessService.Domain;
using MongoDB.Driver;
using RealEstateCrm.Contracts.Paging;

namespace AccessService.Infrastructure.Persistence.Mongo;

/// <summary>
/// Implementación Mongo de <see cref="IUserAccountReadPort"/>: consulta directa a
/// <c>IMongoCollection</c> (mongodb-dotnet-driver skill: <c>IRepository</c> no cubre búsquedas
/// distintas de por <c>_id</c> ni paginación).
/// </summary>
public sealed class UserAccountReadRepository(IMongoDatabase database) : IUserAccountReadPort
{
    private readonly IMongoCollection<UserAccount> _collection = database.GetCollection<UserAccount>("user_accounts");

    public async Task<UserAccount?> GetByKeycloakSubjectAsync(Guid keycloakSubject, CancellationToken cancellationToken = default)
    {
        var filter = Builders<UserAccount>.Filter.Eq(u => u.KeycloakSubject, keycloakSubject);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<UserAccount>> SearchAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var totalCount = await _collection.CountDocumentsAsync(FilterDefinition<UserAccount>.Empty, cancellationToken: cancellationToken);

        var items = await _collection.Find(FilterDefinition<UserAccount>.Empty)
            .SortBy(u => u.DisplayName)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<UserAccount>(items, page, pageSize, totalCount, page * pageSize < totalCount);
    }
}
