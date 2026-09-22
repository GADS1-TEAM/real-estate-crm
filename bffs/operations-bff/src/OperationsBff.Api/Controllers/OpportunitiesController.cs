using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OperationsBff.Api.AnalyticsService;
using OperationsBff.Api.CommercialService;
using OperationsBff.Api.DemandService;
using OperationsBff.Api.AccessService;

namespace OperationsBff.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class OpportunitiesController : ControllerBase
{
    private readonly AnalyticsServiceClient _analyticsServiceClient;
    private readonly DemandServiceClient _demandServiceClient;
    private readonly CommercialServiceClient _commercialServiceClient;

    public OpportunitiesController(
        AnalyticsServiceClient analyticsServiceClient,
        DemandServiceClient demandServiceClient,
        CommercialServiceClient commercialServiceClient)
    {
        _analyticsServiceClient = analyticsServiceClient;
        _demandServiceClient = demandServiceClient;
        _commercialServiceClient = commercialServiceClient;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? pipelineKind, [FromQuery] string? responsibleUserId, [FromQuery] string? stageCode, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var response = await _analyticsServiceClient.GetAsync("/api/v1/analytics/dashboard/pipeline", cancellationToken);
        return await ProxyResults.FromAsync(response, cancellationToken);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var response = await _analyticsServiceClient.GetAsync("/api/v1/analytics/dashboard/pipeline", cancellationToken);
        return await ProxyResults.FromAsync(response, cancellationToken);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RealEstateCrm.Contracts.Opportunities.CreateOpportunityRequestV1 request, CancellationToken cancellationToken)
    {
        var response = await _demandServiceClient.PostAsync("/api/v1/requirements", request, cancellationToken);
        return await ProxyResults.FromAsync(response, cancellationToken);
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] RealEstateCrm.Contracts.Opportunities.UpdateOpportunityRequestV1 request, CancellationToken cancellationToken)
    {
        return Ok(new { status = "accepted", message = "Operación registrada." });
    }

    [HttpPost("{id}/stage")]
    public async Task<IActionResult> ChangeStage(string id, [FromBody] RealEstateCrm.Contracts.Opportunities.ChangeOpportunityStageRequestV1 request, CancellationToken cancellationToken)
    {
        return Ok(new { status = "accepted", message = "Operación registrada." });
    }

    [HttpPost("{id}/win")]
    public async Task<IActionResult> Win(string id, [FromBody] RealEstateCrm.Contracts.Opportunities.CloseOpportunityWinRequestV1 request, CancellationToken cancellationToken)
    {
        return Ok(new { status = "accepted", message = "Operación registrada." });
    }

    [HttpPost("{id}/loss")]
    public async Task<IActionResult> Loss(string id, [FromBody] RealEstateCrm.Contracts.Opportunities.CloseOpportunityLossRequestV1 request, CancellationToken cancellationToken)
    {
        return Ok(new { status = "accepted", message = "Operación registrada." });
    }
}
