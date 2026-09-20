using System.Threading;
using System.Threading.Tasks;

namespace RealEstateCrm.BuildingBlocks.References;

public record ListingReferenceV1(string ListingId, string Status, string OperationTypeCode);

public interface IListingReferencePort
{
    Task<ListingReferenceV1?> GetListingAsync(string listingId, CancellationToken cancellationToken = default);
}
