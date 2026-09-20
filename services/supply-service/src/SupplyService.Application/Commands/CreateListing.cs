using System.Threading;
using System.Threading.Tasks;
using SupplyService.Application.Ports;
using SupplyService.Domain.Aggregates;
using SupplyService.Domain.Exceptions;
using System;
using RealEstateCrm.Contracts.Financial;

namespace SupplyService.Application.Commands;

public record CreateListingCommand(string ListingId, string PropertyId, string OperationTypeCode, string CanonicalTitle, string? CanonicalDescription, ListingCommercialTerms CommercialTerms, ListingAvailability Availability, string? ResponsibleUserId);

public class CreateListingHandler
{
    private readonly IListingRepository _repository;

    public CreateListingHandler(IListingRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(CreateListingCommand command, CancellationToken cancellationToken)
    {
        var listing = Listing.Create(command.ListingId, command.PropertyId, command.OperationTypeCode, command.CanonicalTitle, command.CanonicalDescription, command.CommercialTerms, command.Availability, command.ResponsibleUserId);
        await _repository.SaveAsync(listing, cancellationToken);
    }
}
