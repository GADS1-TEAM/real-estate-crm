using Microsoft.AspNetCore.Mvc;
using DemandService.Application.Commands;
using DemandService.Domain.Aggregates;
using RealEstateCrm.BuildingBlocks.Persistence;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DemandService.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class RequirementsController : ControllerBase
{
    private readonly CreateRequirementHandler _createHandler;
    private readonly IRepository<Requirement, string> _repository;

    public RequirementsController(CreateRequirementHandler createHandler, IRepository<Requirement, string> repository)
    {
        _createHandler = createHandler;
        _repository = repository;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequirementCommand command, CancellationToken ct)
    {
        await _createHandler.HandleAsync(command, ct);
        return Ok(new { requirementId = command.RequirementId, status = "DRAFT" });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var req = await _repository.GetByIdAsync(id, ct);
        if (req == null) return NotFound();
        return Ok(req);
    }

    [HttpGet]
    public IActionResult GetAll(CancellationToken ct)
    {
        return Ok(new List<Requirement>());
    }

    [HttpPost("{id}/selected-listings")]
    public async Task<IActionResult> SelectListing(string id, [FromBody] SelectListingRequest request, CancellationToken ct)
    {
        var req = await _repository.GetByIdAsync(id, ct);
        if (req == null) return NotFound();

        var updated = req.AddSelectedListing(request.ListingId);
        await _repository.UpdateAsync(updated, ct);
        return Ok(updated);
    }
}

public class SelectListingRequest
{
    public string ListingId { get; set; } = string.Empty;
}
