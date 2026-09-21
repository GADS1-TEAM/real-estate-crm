using System;
using System.Collections.Generic;

namespace AnalyticsService.Domain.Entities;

public class MetricDefinition
{
    public string MetricCode { get; set; } = string.Empty;
    public string SemanticDefinition { get; set; } = string.Empty;
    public string FormulaVersion { get; set; } = string.Empty;
    public List<string> SourceEvents { get; set; } = new();
    public List<string> Dimensions { get; set; } = new();
    public DateTime? EffectiveFrom { get; set; }
}
