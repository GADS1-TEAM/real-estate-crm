using AccessService.Domain;
using MongoDB.Driver;
using RealEstateCrm.BuildingBlocks.Infrastructure.Persistence.Mongo;

namespace AccessService.Infrastructure.Persistence.Mongo;

/// <summary>Repositorio de <see cref="UserAccount"/> sobre la colección <c>user_accounts</c> (D8), con concurrencia optimista (V2-ACL-001, pedido explícito).</summary>
public sealed class UserAccountRepository(IMongoDatabase database, MongoSessionAccessor sessionAccessor)
    : VersionedMongoRepository<UserAccount, Guid>(
        database,
        collectionName: "user_accounts",
        sessionAccessor,
        idSelector: user => user.UserId,
        versionSelector: user => user.Version);
