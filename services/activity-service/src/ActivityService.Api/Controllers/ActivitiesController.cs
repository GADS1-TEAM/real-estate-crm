using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ActivityService.Application.Commands;
using ActivityService.Application.Queries;
using RealEstateCrm.Contracts.Activities;

namespace ActivityService.Api.Controllers;

[ApiController]
[Route("api/v1/activities")]
public class ActivitiesController : ControllerBase
{
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
        
        return BadRequest("Must provide at least one filter ID.");
    }
}
