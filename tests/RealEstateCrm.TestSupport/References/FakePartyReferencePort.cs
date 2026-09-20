using System.Collections.Concurrent;
using RealEstateCrm.BuildingBlocks.References;

namespace RealEstateCrm.TestSupport.References;

public sealed class FakePartyReferencePort : IPartyReferencePort
{
    private readonly ConcurrentDictionary<Guid, PartyReferenceV1> _parties = new();

    public void Add(PartyReferenceV1 party) => _parties[party.PartyId] = party;

    public Task<PartyReferenceV1?> GetAsync(Guid partyId, CancellationToken ct = default)
    {
        _parties.TryGetValue(partyId, out var party);
        return Task.FromResult(party);
    }
}
