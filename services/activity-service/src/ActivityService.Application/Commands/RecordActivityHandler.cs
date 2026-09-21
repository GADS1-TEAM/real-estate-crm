using System;
using System.Threading;
using System.Threading.Tasks;
using ActivityService.Domain;
using RealEstateCrm.Contracts.Activities;

namespace ActivityService.Application.Commands;

public class RecordActivityHandler
{
    private readonly IActivityRepository _repository;
    // Missing Event Publisher (outbox)

    public RecordActivityHandler(IActivityRepository repository)
    {
        _repository = repository;
    }

    public async Task<ActivityResponseV1> Handle(RecordActivityRequestV1 request, CancellationToken cancellationToken)
    {
        var activityId = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;
        var activity = new Activity(
            activityId,
            request.ActivityTypeCode,
            request.OccurredAt,
            request.RecordedByUserId,
            request.PrimaryPartyId,
            request.RelatedCompanyId,
            request.RelatedContactId,
            request.PipelineItemId,
            request.PropertyId,
            request.ListingId,
            request.Description,
            request.Result,
            now
        );

        await _repository.AddAsync(activity, cancellationToken);
        
        // Return response
        return new ActivityResponseV1(
            activity.ActivityId,
            activity.ActivityTypeCode,
            activity.OccurredAt,
            activity.RecordedByUserId,
            activity.PrimaryPartyId ?? "",
            activity.RelatedCompanyId,
            activity.RelatedContactId,
            activity.PipelineItemId,
            activity.PropertyId,
            activity.ListingId,
            activity.Description,
            activity.Result,
            activity.CreatedAt
        );
    }
}
