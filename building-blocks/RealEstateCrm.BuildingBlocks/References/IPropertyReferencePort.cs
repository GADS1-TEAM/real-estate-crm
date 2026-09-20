using System.Threading;
using System.Threading.Tasks;
using RealEstateCrm.Contracts.Properties;

namespace RealEstateCrm.BuildingBlocks.References;

public interface IPropertyReferencePort
{
    Task<PropertyReferenceV1?> GetPropertyAsync(string propertyId, CancellationToken cancellationToken = default);
}
