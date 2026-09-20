using System.Threading;
using System.Threading.Tasks;
using SupplyService.Domain.Aggregates;

namespace SupplyService.Application.Ports;

public interface IListingRepository
{
    Task<Listing?> GetByIdAsync(string listingId, CancellationToken cancellationToken);
    Task SaveAsync(Listing listing, CancellationToken cancellationToken);
}
