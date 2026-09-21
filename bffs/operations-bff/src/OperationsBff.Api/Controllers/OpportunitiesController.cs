using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace OperationsBff.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class OpportunitiesController : ControllerBase
{
    public OpportunitiesController()
    {
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? pipelineKind, [FromQuery] string? responsibleUserId, [FromQuery] string? stageCode, [FromQuery] string? status)
    {
        // TODO: Query analytics service read model
        return Ok(new { });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        // TODO: Query analytics service read model
        return Ok(new { });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RealEstateCrm.Contracts.Opportunities.CreateOpportunityRequestV1 request)
    {
        // Routes to demand-service (CreateRequirement) if DEMAND or supply-service (OpenCaptationCase) if SUPPLY.
        return Ok(new { });
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] RealEstateCrm.Contracts.Opportunities.UpdateOpportunityRequestV1 request)
    {
        return Ok(new { });
    }

    [HttpPost("{id}/stage")]
    public async Task<IActionResult> ChangeStage(string id, [FromBody] RealEstateCrm.Contracts.Opportunities.ChangeOpportunityStageRequestV1 request)
    {
        return Ok(new { });
    }

    [HttpPost("{id}/win")]
    public async Task<IActionResult> Win(string id, [FromBody] RealEstateCrm.Contracts.Opportunities.CloseOpportunityWinRequestV1 request)
    {
        return Ok(new { });
    }

    [HttpPost("{id}/loss")]
    public async Task<IActionResult> Loss(string id, [FromBody] RealEstateCrm.Contracts.Opportunities.CloseOpportunityLossRequestV1 request)
    {
        return Ok(new { });
    }
}
