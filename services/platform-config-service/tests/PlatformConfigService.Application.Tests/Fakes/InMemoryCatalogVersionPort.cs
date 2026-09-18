using PlatformConfigService.Application.Ports;

namespace PlatformConfigService.Application.Tests.Fakes;

/// <summary>Implementación en memoria de <see cref="ICatalogVersionPort"/> para tests de Application, sin Mongo real.</summary>
public sealed class InMemoryCatalogVersionPort : ICatalogVersionPort
{
    private readonly Dictionary<string, int> _versions = new();

    public Task<int> GetCurrentAsync(string catalogType, CancellationToken cancellationToken = default) =>
        Task.FromResult(_versions.TryGetValue(catalogType, out var version) ? version : 0);

    public Task<int> IncrementAsync(string catalogType, CancellationToken cancellationToken = default)
    {
        var next = (_versions.TryGetValue(catalogType, out var version) ? version : 0) + 1;
        _versions[catalogType] = next;
        return Task.FromResult(next);
    }
}
