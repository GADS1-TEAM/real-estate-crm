using System;
using System.Collections.Generic;

namespace PropertyService.Domain.Aggregates;

public record Property(
    string PropertyId,
    string PropertyTypeCode,
    PropertyLocation Location,
    decimal? SurfaceM2,
    decimal? SurfaceHa,
    PropertyPhysicalAttributes? PhysicalAttributes,
    PropertyRuralAttributes? RuralAttributes,
    string LifecycleStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int Version)
{
    public static Property Create(string propertyId, string propertyTypeCode, PropertyLocation location, decimal? surfaceM2, decimal? surfaceHa, PropertyPhysicalAttributes? physicalAttributes, PropertyRuralAttributes? ruralAttributes)
    {
        return new Property(propertyId, propertyTypeCode, location, surfaceM2, surfaceHa, physicalAttributes, ruralAttributes, "DRAFT", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 1);
    }

    public Property Update(string lifecycleStatus)
    {
        return this with 
        { 
            LifecycleStatus = lifecycleStatus, 
            UpdatedAt = DateTimeOffset.UtcNow, 
            Version = Version + 1 
        };
    }
}

public record PropertyLocation(string? Province, string? Locality, string? Neighborhood, string? Street, string? StreetNumber, decimal? Latitude, decimal? Longitude);
public record PropertyPhysicalAttributes(int? Environments, int? Bedrooms, int? Bathrooms, int? Age);
public record PropertyRuralAttributes(string? Use, string? Water, string? Access, string? Improvements);
