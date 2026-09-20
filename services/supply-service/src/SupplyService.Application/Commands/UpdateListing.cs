using System.Threading;
using System.Threading.Tasks;
using SupplyService.Application.Ports;
using SupplyService.Domain.Aggregates;
using SupplyService.Domain.Exceptions;
using SupplyService.Application.Events;
using RealEstateCrm.Contracts.Financial;
using System;

namespace SupplyService.Application.Commands;

public record UpdateListingCommand(string ListingId, string CanonicalTitle, string? CanonicalDescription, ListingCommercialTerms CommercialTerms, ListingAvailability Availability, string? ResponsibleUserId);

public class UpdateListingHandler
{
    private readonly IListingRepository _repository;
    private readonly IOutboxPort _outbox;

    public UpdateListingHandler(IListingRepository repository, IOutboxPort outbox)
    {
        _repository = repository;
        _outbox = outbox;
    }

    public async Task Handle(UpdateListingCommand command, CancellationToken cancellationToken)
    {
        var listing = await _repository.GetByIdAsync(command.ListingId, cancellationToken);
        if (listing == null) throw new DomainException("listing_not_found", "Listing not found");

        var updatedListing = listing.Update(command.CanonicalTitle, command.CanonicalDescription, command.CommercialTerms, command.Availability, command.ResponsibleUserId);
        await _repository.SaveAsync(updatedListing, cancellationToken);

        await _outbox.PublishAsync(new ListingUpdatedV1(
            updatedListing.ListingId,
            updatedListing.PropertyId,
            updatedListing.OperationTypeCode,
            null, null, updatedListing.CommercialTerms.Price.ToContract(), null, null, updatedListing.Status
        ), cancellationToken);
    }
}
