using System.Collections.Concurrent;
using RealEstateCrm.BuildingBlocks.Messaging;

namespace RealEstateCrm.TestSupport.Messaging;

public class FakeInbox : IInbox
{
    private readonly ConcurrentDictionary<string, bool> _processed = new();

    public Task<bool> TryMarkConsumedAsync(Guid eventId, string consumerName, CancellationToken cancellationToken = default)
    {
        var key = $"{eventId}_{consumerName}";
        var added = _processed.TryAdd(key, true);
        return Task.FromResult(added);
    }
}
