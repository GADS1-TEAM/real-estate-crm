namespace RealEstateCrm.Contracts.Properties;

public record PropertyReferenceLocationV1(string? Province, string? Locality, string? Neighborhood, string? Street, string? StreetNumber);

public record PropertyReferenceV1(
    string PropertyId,
    string PropertyTypeCode,
    PropertyReferenceLocationV1 Location,
    string Status,
    bool HasActiveInterests);
