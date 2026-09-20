using System.Threading.Tasks;
using System.Threading;

namespace SupplyService.Application.Ports;

public interface IPropertyReferencePort
{
    Task<PropertyReferenceResult> GetPropertyAsync(string propertyId, CancellationToken cancellationToken);
}

public record PropertyReferenceResult(bool Exists, bool HasActiveInterests);

public interface ICatalogReaderPort
{
    Task<bool> IsValidOperationTypeCodeAsync(string operationTypeCode, CancellationToken cancellationToken);
}
