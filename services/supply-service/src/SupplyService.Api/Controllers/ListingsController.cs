using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SupplyService.Application.Commands;
using SupplyService.Application.Ports;
using SupplyService.Domain.Aggregates;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SupplyService.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ListingsController : ControllerBase
{
    static ListingsController()
    {
        if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(Listing)))
        {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<Listing>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }
    }

    private readonly CreateListingHandler _createHandler;
    private readonly UpdateListingHandler _updateHandler;
    private readonly ActivateListingHandler _activateHandler;
    private readonly PauseListingHandler _pauseHandler;
    private readonly CloseListingHandler _closeHandler;
    private readonly IListingRepository _repository;

    public ListingsController(
        CreateListingHandler createHandler,
        UpdateListingHandler updateHandler,
        ActivateListingHandler activateHandler,
        PauseListingHandler pauseHandler,
        CloseListingHandler closeHandler,
        IListingRepository repository)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _activateHandler = activateHandler;
        _pauseHandler = pauseHandler;
        _closeHandler = closeHandler;
        _repository = repository;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateListingCommand command, CancellationToken ct)
    {
        await _createHandler.Handle(command, ct);
        return Ok(new { listingId = command.ListingId, status = "DRAFT" });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateListingCommand command, CancellationToken ct)
    {
        var cmd = command with { ListingId = id };
        await _updateHandler.Handle(cmd, ct);
        return Ok();
    }

    [HttpPost("{id}/activate")]
    public async Task<IActionResult> Activate(string id, CancellationToken ct)
    {
        await _activateHandler.Handle(new ActivateListingCommand(id), ct);
        return Ok();
    }

    [HttpPost("{id}/pause")]
    public async Task<IActionResult> Pause(string id, CancellationToken ct)
    {
        await _pauseHandler.Handle(new PauseListingCommand(id), ct);
        return Ok();
    }

    [HttpPost("{id}/close")]
    public async Task<IActionResult> Close(string id, CancellationToken ct)
    {
        await _closeHandler.Handle(new CloseListingCommand(id), ct);
        return Ok();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var listing = await _repository.GetByIdAsync(id, ct);
        if (listing == null) return NotFound();
        return Ok(listing);
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromServices] IMongoDatabase database, CancellationToken ct)
    {
        var listings = await database.GetCollection<Listing>("Listings").Find(Builders<Listing>.Filter.Empty).ToListAsync(ct);
        return Ok(listings);
    }
}
