using System.Threading;
using System.Threading.Tasks;
using SupplyService.Application.Ports;
using SupplyService.Domain.Exceptions;
using SupplyService.Application.Events;

namespace SupplyService.Application.Commands;

public record CloseListingCommand(string ListingId);

public class CloseListingHandler
{
    private readonly IListingRepository _repository;
    private readonly IOutboxPort _outbox;

    public CloseListingHandler(IListingRepository repository, IOutboxPort outbox)
    {
        _repository = repository;
        _outbox = outbox;
    }

    public async Task Handle(CloseListingCommand command, CancellationToken cancellationToken)
    {
        var listing = await _repository.GetByIdAsync(command.ListingId, cancellationToken);
        if (listing == null) throw new DomainException("listing_not_found", "Listing not found");

        var closedListing = listing.Close();
        await _repository.SaveAsync(closedListing, cancellationToken);

        await _outbox.PublishAsync(new ListingClosedV1(
            closedListing.ListingId,
            closedListing.PropertyId,
            closedListing.OperationTypeCode,
            null, null, closedListing.CommercialTerms.Price.ToContract(), null, null, closedListing.Status
        ), cancellationToken);
    }
}
