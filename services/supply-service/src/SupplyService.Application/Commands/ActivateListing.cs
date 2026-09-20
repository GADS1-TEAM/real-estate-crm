using System.Threading;
using System.Threading.Tasks;
using SupplyService.Application.Ports;
using SupplyService.Domain.Aggregates;
using SupplyService.Domain.Exceptions;
using SupplyService.Application.Events;

namespace SupplyService.Application.Commands;

public record ActivateListingCommand(string ListingId);

public class ActivateListingHandler
{
    private readonly IListingRepository _repository;
    private readonly IPropertyReferencePort _propertyPort;
    private readonly ICatalogReaderPort _catalogPort;
    private readonly IOutboxPort _outbox;

    public ActivateListingHandler(IListingRepository repository, IPropertyReferencePort propertyPort, ICatalogReaderPort catalogPort, IOutboxPort outbox)
    {
        _repository = repository;
        _propertyPort = propertyPort;
        _catalogPort = catalogPort;
        _outbox = outbox;
    }

    public async Task Handle(ActivateListingCommand command, CancellationToken cancellationToken)
    {
        var listing = await _repository.GetByIdAsync(command.ListingId, cancellationToken);
        if (listing == null) throw new DomainException("listing_not_found", "Listing not found");

        var propertyInfo = await _propertyPort.GetPropertyAsync(listing.PropertyId, cancellationToken);
        if (!propertyInfo.Exists)
        {
            throw new DomainException("property_reference_not_found", "Property reference not found");
        }
        if (!propertyInfo.HasActiveInterests)
        {
            throw new DomainException("property_has_no_active_interests", "Property has no active interests");
        }

        var isOpValid = await _catalogPort.IsValidOperationTypeCodeAsync(listing.OperationTypeCode, cancellationToken);
        if (!isOpValid)
        {
            throw new DomainException("invalid_operation_type_code", "Operation type code is invalid");
        }

        var activatedListing = listing.Activate();
        await _repository.SaveAsync(activatedListing, cancellationToken);

        await _outbox.PublishAsync(new ListingActivatedV1(
            activatedListing.ListingId,
            activatedListing.PropertyId,
            activatedListing.OperationTypeCode,
            null, // PropertyTypeCode
            null, // Location
            activatedListing.CommercialTerms.Price.ToContract(),
            null, // Surface
            null, // Environments
            activatedListing.Status
        ), cancellationToken);
    }
}
