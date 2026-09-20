using RealEstateCrm.Contracts.Events;
namespace RealEstateCrm.Contracts.Properties;

public record PropertyRegisteredV1(string PropertyId, string PropertyTypeCode, string LifecycleStatus, string? Province, string? Locality);
public record PropertyUpdatedV1(string PropertyId, string LifecycleStatus);
public record PropertyInterestAddedV1(string PropertyId, string InterestId, string HolderPartyId, string RightType);
