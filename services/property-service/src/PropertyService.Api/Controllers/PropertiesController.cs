using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using PropertyService.Application;
using PropertyService.Domain.Aggregates;
using RealEstateCrm.Contracts.Properties;
using RealEstateCrm.BuildingBlocks.Persistence;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PropertyService.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PropertiesController : ControllerBase
{
    static PropertiesController()
    {
        if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(Property)))
        {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<Property>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }
    }

    private readonly PropertyManagementService _service;
    private readonly IRepository<Property, string> _propertyRepository;
    private readonly IRepository<PropertyInterest, string> _propertyInterestRepository;

    public PropertiesController(PropertyManagementService service, IRepository<Property, string> propertyRepository, IRepository<PropertyInterest, string> propertyInterestRepository)
    {
        _service = service;
        _propertyRepository = propertyRepository;
        _propertyInterestRepository = propertyInterestRepository;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePropertyRequest request,
        CancellationToken cancellationToken)
    {
        var property = await _service.CreatePropertyAsync(
            request.PropertyId,
            request.PropertyTypeCode,
            request.Location,
            request.SurfaceM2,
            request.SurfaceHa,
            request.PhysicalAttributes,
            request.RuralAttributes,
            Guid.NewGuid(), // Fake actor
            cancellationToken);

        return Ok(property);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdatePropertyRequest request,
        CancellationToken cancellationToken)
    {
        var property = await _service.UpdatePropertyAsync(
            id,
            request.LifecycleStatus,
            Guid.NewGuid(), // Fake actor
            cancellationToken);

        return Ok(property);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var property = await _propertyRepository.GetByIdAsync(id, cancellationToken);
        if (property == null) return NotFound();
        return Ok(property);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromServices] IMongoDatabase database, CancellationToken cancellationToken)
    {
        var properties = await database.GetCollection<Property>("Properties").Find(Builders<Property>.Filter.Empty).ToListAsync(cancellationToken);
        return Ok(properties);
    }

    [HttpPost("{id}/interests")]
    public async Task<IActionResult> AddInterest(
        string id,
        [FromBody] AddPropertyInterestRequest request,
        CancellationToken cancellationToken)
    {
        var interest = await _service.AddPropertyInterestAsync(
            id,
            request.InterestId,
            request.HolderPartyId,
            request.RightType,
            request.Participation,
            Guid.NewGuid(), // Fake actor
            cancellationToken);

        return Ok(interest);
    }

    [HttpGet("{id}/interests")]
    public async Task<IActionResult> GetInterests(string id, CancellationToken cancellationToken)
    {
        // Simple mock, would normally read from a read repo
        return Ok(new List<PropertyInterest>());
    }

    [HttpGet("{id}/reference")]
    public async Task<IActionResult> GetReference(string id, CancellationToken cancellationToken)
    {
        var property = await _propertyRepository.GetByIdAsync(id, cancellationToken);
        if (property == null) return NotFound();

        var reference = new PropertyReferenceV1(
            property.PropertyId,
            property.PropertyTypeCode,
            new PropertyReferenceLocationV1(
                property.Location?.Province,
                property.Location?.Locality,
                property.Location?.Neighborhood,
                property.Location?.Street,
                property.Location?.StreetNumber),
            property.LifecycleStatus,
            false // HasActiveInterests normally evaluated
        );
        return Ok(reference);
    }
}

public class CreatePropertyRequest
{
    public string PropertyId { get; set; } = string.Empty;
    public string PropertyTypeCode { get; set; } = string.Empty;
    public PropertyLocation Location { get; set; } = null!;
    public decimal? SurfaceM2 { get; set; }
    public decimal? SurfaceHa { get; set; }
    public PropertyPhysicalAttributes? PhysicalAttributes { get; set; }
    public PropertyRuralAttributes? RuralAttributes { get; set; }
}

public class UpdatePropertyRequest
{
    public string LifecycleStatus { get; set; } = string.Empty;
}

public class AddPropertyInterestRequest
{
    public string InterestId { get; set; } = string.Empty;
    public string HolderPartyId { get; set; } = string.Empty;
    public string RightType { get; set; } = string.Empty;
    public decimal? Participation { get; set; }
}
