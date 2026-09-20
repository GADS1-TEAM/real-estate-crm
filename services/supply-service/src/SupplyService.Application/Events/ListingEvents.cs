using RealEstateCrm.Contracts.Financial;

namespace SupplyService.Application.Events;

public record ListingCreatedV1(string ListingId, string PropertyId, string OperationTypeCode, string? PropertyTypeCode, object? Location, MoneyV1? Price, decimal? Surface, int? Environments, string Status);
public record ListingActivatedV1(string ListingId, string PropertyId, string OperationTypeCode, string? PropertyTypeCode, object? Location, MoneyV1? Price, decimal? Surface, int? Environments, string Status);
public record ListingPausedV1(string ListingId, string PropertyId, string OperationTypeCode, string? PropertyTypeCode, object? Location, MoneyV1? Price, decimal? Surface, int? Environments, string Status);
public record ListingClosedV1(string ListingId, string PropertyId, string OperationTypeCode, string? PropertyTypeCode, object? Location, MoneyV1? Price, decimal? Surface, int? Environments, string Status);
public record ListingUpdatedV1(string ListingId, string PropertyId, string OperationTypeCode, string? PropertyTypeCode, object? Location, MoneyV1? Price, decimal? Surface, int? Environments, string Status);
