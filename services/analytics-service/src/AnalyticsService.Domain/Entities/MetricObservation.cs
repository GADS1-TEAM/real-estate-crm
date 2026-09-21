using System;
using System.Collections.Generic;

namespace AnalyticsService.Domain.Entities;

public class MetricObservation
{
    public string MetricCode { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public Dictionary<string, string> Dimensions { get; set; } = new();
    public decimal Value { get; set; }
    
    // Invariant: If data is missing or not applicable, availability MUST be UNKNOWN
    // (never convert missing to 0!). 0 is strictly reserved for valid calculation resulting in zero count.
    public Availability Availability { get; set; } = Availability.UNKNOWN;
    
    public string SourceProjectionVersion { get; set; } = string.Empty;
    public List<string> LineageRefs { get; set; } = new();
    public DateTime CalculatedAt { get; set; }
    
    public void SetValue(decimal value)
    {
        Value = value;
        Availability = Availability.KNOWN;
    }
    
    public void SetUnknown()
    {
        Value = 0; // Value shouldn't be read if Availability is UNKNOWN
        Availability = Availability.UNKNOWN;
    }
}
