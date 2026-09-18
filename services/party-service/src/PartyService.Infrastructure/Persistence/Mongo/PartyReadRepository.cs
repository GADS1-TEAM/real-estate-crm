using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using PartyService.Application.Ports;
using PartyService.Domain;
using RealEstateCrm.Contracts.Paging;

namespace PartyService.Infrastructure.Persistence.Mongo;

/// <summary>
/// Implementación Mongo de <see cref="IPartyReadPort"/>. La búsqueda de texto usa una regex
/// escapada (<see cref="Regex.Escape(string)"/>: el input del usuario nunca se interpreta como
/// patrón, sin inyección NoSQL) sin distinguir mayúsculas.
/// </summary>
public sealed class PartyReadRepository(IMongoDatabase database) : IPartyReadPort
{
    private readonly IMongoCollection<Party> _parties = database.GetCollection<Party>(PartyCollections.Parties);
    private readonly IMongoCollection<PartyRelationship> _relationships = database.GetCollection<PartyRelationship>(PartyCollections.PartyRelationships);

    public async Task<PageV1<Party>> SearchAsync(PartySearchCriteria criteria, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var builder = Builders<Party>.Filter;
        var filters = new List<FilterDefinition<Party>>();

        if (!string.IsNullOrWhiteSpace(criteria.Query))
        {
            var pattern = new BsonRegularExpression(Regex.Escape(criteria.Query.Trim()), "i");

            filters.Add(builder.Or(
                builder.Regex(p => p.Profile.DisplayName, pattern),
                builder.Regex(p => p.Profile.Email, pattern),
                builder.Regex(p => p.Profile.Phone, pattern),
                builder.Regex(p => p.Profile.TaxIdentifier, pattern)));
        }

        if (criteria.Kind is { } kind)
        {
            filters.Add(builder.Eq(p => p.Kind, kind));
        }

        if (criteria.CommercialStatus is { } status)
        {
            filters.Add(builder.Eq(p => p.CommercialStatus, status));
        }

        if (criteria.ResponsibleUserId is { } responsible)
        {
            filters.Add(builder.Eq(p => p.ResponsibleUserId, responsible));
        }

        var filter = filters.Count == 0 ? FilterDefinition<Party>.Empty : builder.And(filters);

        var totalCount = await _parties.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var items = await _parties.Find(filter)
            .SortBy(p => p.Profile.DisplayName)
            .ThenBy(p => p.PartyId)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PageV1<Party>(items, page, pageSize, totalCount);
    }

    public async Task<IReadOnlyList<Party>> GetManyAsync(IReadOnlyCollection<Guid> partyIds, CancellationToken cancellationToken = default)
    {
        if (partyIds.Count == 0)
        {
            return Array.Empty<Party>();
        }

        return await _parties.Find(Builders<Party>.Filter.In(p => p.PartyId, partyIds)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PartyRelationship>> ListRelationshipsAsync(Guid partyId, CancellationToken cancellationToken = default)
    {
        var builder = Builders<PartyRelationship>.Filter;
        var filter = builder.Or(builder.Eq(r => r.FromPartyId, partyId), builder.Eq(r => r.ToPartyId, partyId));

        return await _relationships.Find(filter).SortBy(r => r.ValidFrom).ToListAsync(cancellationToken);
    }

    public async Task<bool> RelationshipExistsAsync(Guid contactId, Guid companyId, RelationshipType type, CancellationToken cancellationToken = default)
    {
        var builder = Builders<PartyRelationship>.Filter;
        var filter = builder.And(
            builder.Eq(r => r.FromPartyId, contactId),
            builder.Eq(r => r.ToPartyId, companyId),
            builder.Eq(r => r.RelationshipType, type),
            builder.Eq(r => r.ValidTo, null));

        return await _relationships.CountDocumentsAsync(filter, cancellationToken: cancellationToken) > 0;
    }
}
