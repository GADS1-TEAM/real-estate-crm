namespace RealEstateCrm.Contracts.Events.Matching;

public record MatchCalculatedV1(
    string MatchId,
    string RequirementId,
    string ListingId,
    string Eligibility,
    int Score);

public record MatchPresentedV1(
    string MatchId,
    string RequirementId,
    string ListingId);

public record MatchSelectedV1(
    string MatchId,
    string RequirementId,
    string ListingId);

public record MatchDiscardedV1(
    string MatchId,
    string RequirementId,
    string ListingId,
    string ReasonCode);

public record MatchInvalidatedV1(
    string MatchId,
    string RequirementId,
    string ListingId);
