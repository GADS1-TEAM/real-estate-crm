using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using PartyService.Domain;

namespace PartyService.Infrastructure.Persistence.Mongo;

/// <summary>
/// Crea, de forma idempotente, índices SOLO de búsqueda. Ninguno es <c>Unique</c>: V2-PTY-001
/// excluye la unicidad de CUIT/email/teléfono y la resolución de duplicados.
/// </summary>
public sealed class PartyServiceIndexInitializer(IMongoDatabase database) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var parties = database.GetCollection<Party>(PartyCollections.Parties);
        var partyKeys = Builders<Party>.IndexKeys;

        await parties.Indexes.CreateManyAsync(
            new[]
            {
                new CreateIndexModel<Party>(partyKeys.Ascending(p => p.Profile.DisplayName), new CreateIndexOptions { Name = "ix_parties_display_name" }),
                new CreateIndexModel<Party>(partyKeys.Ascending(p => p.Profile.Email), new CreateIndexOptions { Name = "ix_parties_email" }),
                new CreateIndexModel<Party>(partyKeys.Ascending(p => p.Profile.Phone), new CreateIndexOptions { Name = "ix_parties_phone" }),
                new CreateIndexModel<Party>(partyKeys.Ascending(p => p.Profile.TaxIdentifier), new CreateIndexOptions { Name = "ix_parties_tax_identifier" }),
                new CreateIndexModel<Party>(partyKeys.Ascending(p => p.ResponsibleUserId), new CreateIndexOptions { Name = "ix_parties_responsible" }),
            },
            cancellationToken);

        var relationships = database.GetCollection<PartyRelationship>(PartyCollections.PartyRelationships);
        var relationshipKeys = Builders<PartyRelationship>.IndexKeys;

        await relationships.Indexes.CreateManyAsync(
            new[]
            {
                new CreateIndexModel<PartyRelationship>(relationshipKeys.Ascending(r => r.FromPartyId), new CreateIndexOptions { Name = "ix_party_relationships_from" }),
                new CreateIndexModel<PartyRelationship>(relationshipKeys.Ascending(r => r.ToPartyId), new CreateIndexOptions { Name = "ix_party_relationships_to" }),
            },
            cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
