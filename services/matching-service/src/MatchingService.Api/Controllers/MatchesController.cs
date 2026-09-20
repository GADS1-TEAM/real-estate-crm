using Microsoft.AspNetCore.Mvc;
using MatchingService.Domain.Aggregates;
using MatchingService.Domain.Services;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MatchingService.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class MatchesController : ControllerBase
{
    [HttpGet]
    public IActionResult GetMatches([FromQuery] string? requirementId, [FromQuery] string? listingId, CancellationToken ct)
    {
        var sampleReq = new MatchingScoringEngine.RequirementSnapshot(
            Id: requirementId ?? "req-1",
            OperationTypeCode: "VENTA",
            PropertyTypeCode: "DEPARTAMENTO",
            Province: "Buenos Aires",
            Locality: "Palermo",
            MaxBudget: 150000m,
            Currency: "USD",
            MinSurfaceArea: 50m,
            MinEnvironments: 2,
            Neighborhood: "Palermo Soho"
        );

        var sampleListing = new MatchingScoringEngine.ListingSnapshot(
            Id: listingId ?? "lst-101",
            OperationTypeCode: "VENTA",
            PropertyTypeCode: "DEPARTAMENTO",
            Province: "Buenos Aires",
            Locality: "Palermo",
            Price: 145000m,
            Currency: "USD",
            SurfaceArea: 55m,
            Environments: 2,
            Neighborhood: "Palermo Soho"
        );

        var matchCase = MatchingScoringEngine.Calculate(sampleReq, sampleListing);

        return Ok(new[] { matchCase });
    }

    [HttpGet("{id}")]
    public IActionResult GetById(string id, CancellationToken ct)
    {
        var sampleReq = new MatchingScoringEngine.RequirementSnapshot(
            Id: "req-1",
            OperationTypeCode: "VENTA",
            PropertyTypeCode: "DEPARTAMENTO",
            Province: "Buenos Aires",
            Locality: "Palermo",
            MaxBudget: 150000m,
            Currency: "USD",
            MinSurfaceArea: 50m,
            MinEnvironments: 2,
            Neighborhood: "Palermo Soho"
        );

        var sampleListing = new MatchingScoringEngine.ListingSnapshot(
            Id: "lst-101",
            OperationTypeCode: "VENTA",
            PropertyTypeCode: "DEPARTAMENTO",
            Province: "Buenos Aires",
            Locality: "Palermo",
            Price: 145000m,
            Currency: "USD",
            SurfaceArea: 55m,
            Environments: 2,
            Neighborhood: "Palermo Soho"
        );

        var matchCase = MatchingScoringEngine.Calculate(sampleReq, sampleListing);
        return Ok(matchCase);
    }
}
