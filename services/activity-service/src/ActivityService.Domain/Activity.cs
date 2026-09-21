using System;

namespace ActivityService.Domain;

public class Activity
{
    public string ActivityId { get; private set; }
    public string ActivityTypeCode { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string RecordedByUserId { get; private set; }
    public string? PrimaryPartyId { get; private set; }
    public string? RelatedCompanyId { get; private set; }
    public string? RelatedContactId { get; private set; }
    public string? PipelineItemId { get; private set; }
    public string? PropertyId { get; private set; }
    public string? ListingId { get; private set; }
    public string Description { get; private set; }
    public string? Result { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Activity() 
    { 
        ActivityId = default!;
        ActivityTypeCode = default!;
        RecordedByUserId = default!;
        Description = default!;
    }

    public Activity(
        string activityId,
        string activityTypeCode,
        DateTimeOffset occurredAt,
        string recordedByUserId,
        string? primaryPartyId,
        string? relatedCompanyId,
        string? relatedContactId,
        string? pipelineItemId,
        string? propertyId,
        string? listingId,
        string description,
        string? result,
        DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(recordedByUserId)) throw new ArgumentException("RecordedByUserId is required.");
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description is required.");
        if (string.IsNullOrWhiteSpace(primaryPartyId) && string.IsNullOrWhiteSpace(pipelineItemId))
            throw new ArgumentException("Must be linked to at least a Party (PrimaryPartyId) or a PipelineItemId.");
            
        ActivityId = activityId;
        ActivityTypeCode = activityTypeCode;
        OccurredAt = occurredAt;
        RecordedByUserId = recordedByUserId;
        PrimaryPartyId = primaryPartyId;
        RelatedCompanyId = relatedCompanyId;
        RelatedContactId = relatedContactId;
        PipelineItemId = pipelineItemId;
        PropertyId = propertyId;
        ListingId = listingId;
        Description = description;
        Result = result;
        CreatedAt = createdAt;
    }
}
