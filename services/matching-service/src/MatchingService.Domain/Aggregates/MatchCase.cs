using System;
using System.Collections.Generic;
using System.Linq;

namespace MatchingService.Domain.Aggregates;

public enum MatchEligibility
{
    INELIGIBLE,
    SOFT_MATCH,
    ELIGIBLE
}

public enum MatchStatus
{
    PRESENTED,
    SELECTED,
    DISCARDED,
    STALE
}

public record HardCriterionResult(string CriterionName, bool Passed, string Reason);

public record SoftCriterionResult(string CriterionName, int Weight, int PointsEarned, int MaxPoints, string Reason);

public record MatchCase
{
    public string MatchId { get; init; } = string.Empty;
    public string RequirementId { get; init; } = string.Empty;
    public string ListingId { get; init; } = string.Empty;
    public MatchEligibility Eligibility { get; init; }
    public int Score { get; init; }
    public IReadOnlyList<HardCriterionResult> HardCriteriaResults { get; init; } = Array.Empty<HardCriterionResult>();
    public IReadOnlyList<SoftCriterionResult> SoftCriteriaResults { get; init; } = Array.Empty<SoftCriterionResult>();
    public IReadOnlyList<string> Explanation { get; init; } = Array.Empty<string>();
    public DateTimeOffset CalculatedAt { get; init; }
    public string ScoringPolicyVersion { get; init; } = "v1";
    public MatchStatus Status { get; init; }
    public string? FeedbackReasonCode { get; init; }
    public long Version { get; init; }

    public MatchCase Select()
    {
        if (Status != MatchStatus.PRESENTED)
        {
            throw new InvalidOperationException($"Cannot select a match in status {Status}");
        }
        return this with { Status = MatchStatus.SELECTED, Version = Version + 1 };
    }

    public MatchCase Discard(string feedbackReasonCode)
    {
        if (Status != MatchStatus.PRESENTED)
        {
            throw new InvalidOperationException($"Cannot discard a match in status {Status}");
        }
        return this with { Status = MatchStatus.DISCARDED, FeedbackReasonCode = feedbackReasonCode, Version = Version + 1 };
    }

    public MatchCase MarkStale()
    {
        if (Status == MatchStatus.STALE)
        {
            return this;
        }
        return this with { Status = MatchStatus.STALE, Version = Version + 1 };
    }
}
