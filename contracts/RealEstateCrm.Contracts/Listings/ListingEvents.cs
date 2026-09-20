using RealEstateCrm.Contracts.Events;
using RealEstateCrm.Contracts.Financial;
namespace RealEstateCrm.Contracts.Listings;

public record ListingCreatedV1(string ListingId, string PropertyId, string OperationTypeCode, string Status);
public record ListingActivatedV1(string ListingId, string Status);
public record ListingPausedV1(string ListingId, string Status);
public record ListingClosedV1(string ListingId, string Status);
public record ListingUpdatedV1(
    string ListingId,
    string PropertyId,
    string OperationTypeCode,
    string PropertyTypeCode,
    string? Location,
    MoneyV1 Price,
    decimal? Surface,
    int? Environments,
    string Status);
