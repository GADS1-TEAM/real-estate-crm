using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RealEstateCrm.BuildingBlocks.References;
using RealEstateCrm.Contracts.Properties;

namespace RealEstateCrm.TestSupport.Fakes;

public class FakePropertyReferencePort : IPropertyReferencePort
{
    private readonly ConcurrentDictionary<string, PropertyReferenceV1> _properties = new();

    public void AddProperty(PropertyReferenceV1 property) => _properties[property.PropertyId] = property;

    public Task<PropertyReferenceV1?> GetPropertyAsync(string propertyId, CancellationToken cancellationToken = default)
    {
        _properties.TryGetValue(propertyId, out var property);
        return Task.FromResult(property);
    }
}
