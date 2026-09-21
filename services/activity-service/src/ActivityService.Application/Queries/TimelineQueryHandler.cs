using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActivityService.Domain;
using RealEstateCrm.Contracts.Activities;

namespace ActivityService.Application.Queries;

public class TimelineQueryHandler
{
    private readonly IActivityRepository _repository;

    public TimelineQueryHandler(IActivityRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<TimelineEntryV1>> GetPartyTimelineAsync(string partyId, CancellationToken cancellationToken)
    {
        var activities = await _repository.GetByPartyIdAsync(partyId, cancellationToken);
        return MapToTimeline(activities);
    }

    public async Task<IEnumerable<TimelineEntryV1>> GetCompanyTimelineAsync(string companyId, CancellationToken cancellationToken)
    {
        var activities = await _repository.GetByCompanyIdAsync(companyId, cancellationToken);
        return MapToTimeline(activities);
    }

    public async Task<IEnumerable<TimelineEntryV1>> GetContactTimelineAsync(string contactId, CancellationToken cancellationToken)
    {
        var activities = await _repository.GetByContactIdAsync(contactId, cancellationToken);
        return MapToTimeline(activities);
    }

    public async Task<IEnumerable<TimelineEntryV1>> GetCommercialTimelineAsync(string pipelineItemId, CancellationToken cancellationToken)
    {
        var activities = await _repository.GetByPipelineItemIdAsync(pipelineItemId, cancellationToken);
        return MapToTimeline(activities);
    }

    private IEnumerable<TimelineEntryV1> MapToTimeline(IEnumerable<Activity> activities)
    {
        return activities
            .OrderByDescending(a => a.OccurredAt)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => new TimelineEntryV1(
                a.ActivityId,
                "ACTIVITY",
                a.OccurredAt,
                a.CreatedAt,
                a.Description,
                new { a.ActivityTypeCode, a.Result, a.RecordedByUserId }
            ));
    }
}
