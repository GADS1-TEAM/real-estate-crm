using MongoDB.Driver;
using PartyService.Domain;
using RealEstateCrm.BuildingBlocks.Infrastructure.Persistence.Mongo;

namespace PartyService.Infrastructure.Persistence.Mongo;

/// <summary>Repositorio de <see cref="Party"/> sobre <c>parties</c> (D8), con concurrencia optimista por <c>Version</c> (conflicto → 409).</summary>
public sealed class PartyRepository(IMongoDatabase database, MongoSessionAccessor sessionAccessor)
    : VersionedMongoRepository<Party, Guid>(
        database,
        collectionName: PartyCollections.Parties,
        sessionAccessor,
        idSelector: party => party.PartyId,
        versionSelector: party => party.Version);

/// <summary>Repositorio de <see cref="PartyRelationship"/> sobre <c>party_relationships</c> (D8). Solo inserciones.</summary>
public sealed class PartyRelationshipRepository(IMongoDatabase database, MongoSessionAccessor sessionAccessor)
    : MongoRepository<PartyRelationship, Guid>(
        database,
        collectionName: PartyCollections.PartyRelationships,
        sessionAccessor,
        idSelector: relationship => relationship.RelationshipId);

internal static class PartyCollections
{
    public const string Parties = "parties";
    public const string PartyRelationships = "party_relationships";
}
