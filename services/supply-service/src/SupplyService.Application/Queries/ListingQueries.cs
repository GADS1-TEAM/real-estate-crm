using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupplyService.Domain.Aggregates;

namespace SupplyService.Application.Queries;

public record SearchListingsQuery(string? Status);
public record GetListingDetailQuery(string ListingId);
public record GetProductCatalogQuery();

public interface IListingQueryService
{
    Task<IEnumerable<Listing>> SearchListingsAsync(SearchListingsQuery query, CancellationToken cancellationToken);
    Task<Listing?> GetListingDetailAsync(GetListingDetailQuery query, CancellationToken cancellationToken);
    Task<IEnumerable<object>> GetProductCatalogAsync(GetProductCatalogQuery query, CancellationToken cancellationToken);
}
