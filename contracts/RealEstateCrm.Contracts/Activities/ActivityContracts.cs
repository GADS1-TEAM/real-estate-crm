namespace RealEstateCrm.Contracts.Activities;

public record ActivityRecordedV1(
    string ActivityId,
    string ActivityTypeCode,
    DateTimeOffset OccurredAt,
    string RecordedByUserId,
    string PrimaryPartyId,
    string? RelatedCompanyId,
    string? RelatedContactId,
    string? PipelineItemId,
    string? PropertyId,
    string? ListingId,
    string Description,
    string? Result,
    DateTimeOffset CreatedAt);

public record RecordActivityRequestV1(
    string ActivityTypeCode,
    DateTimeOffset OccurredAt,
    string RecordedByUserId,
    string PrimaryPartyId,
    string? RelatedCompanyId,
    string? RelatedContactId,
    string? PipelineItemId,
    string? PropertyId,
    string? ListingId,
    string Description,
    string? Result);

public record ActivityResponseV1(
    string ActivityId,
    string ActivityTypeCode,
    DateTimeOffset OccurredAt,
    string RecordedByUserId,
    string PrimaryPartyId,
    string? RelatedCompanyId,
    string? RelatedContactId,
    string? PipelineItemId,
    string? PropertyId,
    string? ListingId,
    string Description,
    string? Result,
    DateTimeOffset CreatedAt);

public record TimelineEntryV1(
    string EntryId,
    string EntryType, // "ACTIVITY" or "STAGE_CHANGE"
    DateTimeOffset OccurredAt,
    DateTimeOffset CreatedAt,
    string Description,
    object Data);
