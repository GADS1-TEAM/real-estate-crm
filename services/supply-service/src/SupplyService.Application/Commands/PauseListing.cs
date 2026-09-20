using System.Threading;
using System.Threading.Tasks;
using SupplyService.Application.Ports;
using SupplyService.Domain.Exceptions;
using SupplyService.Application.Events;

namespace SupplyService.Application.Commands;

public record PauseListingCommand(string ListingId);

public class PauseListingHandler
{
    private readonly IListingRepository _repository;
    private readonly IOutboxPort _outbox;

    public PauseListingHandler(IListingRepository repository, IOutboxPort outbox)
    {
        _repository = repository;
        _outbox = outbox;
    }

    public async Task Handle(PauseListingCommand command, CancellationToken cancellationToken)
    {
        var listing = await _repository.GetByIdAsync(command.ListingId, cancellationToken);
        if (listing == null) throw new DomainException("listing_not_found", "Listing not found");

        var pausedListing = listing.Pause();
        await _repository.SaveAsync(pausedListing, cancellationToken);

        await _outbox.PublishAsync(new ListingPausedV1(
            pausedListing.ListingId,
            pausedListing.PropertyId,
            pausedListing.OperationTypeCode,
            null, null, pausedListing.CommercialTerms.Price.ToContract(), null, null, pausedListing.Status
        ), cancellationToken);
    }
}
