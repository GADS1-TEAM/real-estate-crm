using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using ActivityService.Domain;
using ActivityService.Application.Commands;
using ActivityService.Application.Queries;
using RealEstateCrm.Contracts.Activities;

namespace ActivityService.Api.Controllers;

[ApiController]
[Route("api/v1/activities")]
public class ActivitiesController : ControllerBase
{
    static ActivitiesController()
    {
        if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(Activity)))
        {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<Activity>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }
    }

    private readonly RecordActivityHandler _recordHandler;
    private readonly TimelineQueryHandler _timelineHandler;

    public ActivitiesController(RecordActivityHandler recordHandler, TimelineQueryHandler timelineHandler)
    {
        _recordHandler = recordHandler;
        _timelineHandler = timelineHandler;
    }

    [HttpPost]
    public async Task<IActionResult> RecordActivity([FromBody] RecordActivityRequestV1 request, CancellationToken cancellationToken)
    {
        var response = await _recordHandler.Handle(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("timeline")]
    public async Task<IActionResult> GetTimeline(
        [FromServices] IMongoDatabase database,
        [FromQuery] string? partyId,
        [FromQuery] string? companyId,
        [FromQuery] string? contactId,
        [FromQuery] string? pipelineItemId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(partyId))
        {
            return Ok(await _timelineHandler.GetPartyTimelineAsync(partyId, cancellationToken));
        }
        if (!string.IsNullOrEmpty(companyId))
        {
            return Ok(await _timelineHandler.GetCompanyTimelineAsync(companyId, cancellationToken));
        }
        if (!string.IsNullOrEmpty(contactId))
        {
            return Ok(await _timelineHandler.GetContactTimelineAsync(contactId, cancellationToken));
        }
        if (!string.IsNullOrEmpty(pipelineItemId))
        {
            return Ok(await _timelineHandler.GetCommercialTimelineAsync(pipelineItemId, cancellationToken));
        }
        
        var activities = await database.GetCollection<Activity>("crm_activity").Find(Builders<Activity>.Filter.Empty).ToListAsync(cancellationToken);
        return Ok(activities);
    }
}
