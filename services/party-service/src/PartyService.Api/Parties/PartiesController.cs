using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartyService.Api.ErrorHandling;
using PartyService.Api.ExecutionContextResolution;
using PartyService.Application;
using PartyService.Application.Parties;
using PartyService.Application.Ports;
using PartyService.Domain;
using RealEstateCrm.BuildingBlocks.Authorization;
using RealEstateCrm.Contracts.Authorization;
using RealEstateCrm.Contracts.Context;
using RealEstateCrm.Contracts.Errors;
using RealEstateCrm.Contracts.Parties;

namespace PartyService.Api.Parties;

/// <summary>
/// PTY-001..PTY-007 (V2-PTY-001). Controller delgado: valida en el borde, traduce la decisión de
/// <see cref="IAuthorizationPort"/> a 403 y delega en <see cref="PartyManagementService"/>, que
/// aplica la regla de propiedad del registro (D2). Empresa y Contacto tienen rutas distintas
/// (<c>/companies</c>, <c>/contacts</c>) pero son la misma Party; <c>/parties</c> agrupa lo común.
/// </summary>
[ApiController]
[Route("api/v1")]
[Authorize]
public sealed class PartiesController(
    PartyManagementService partyService,
    IAuthorizationPort authorizationPort,
    CurrentExecutionContextProvider executionContextProvider) : ControllerBase
{
    // ---------- Queries ----------

    [HttpGet("parties")]
    public async Task<IActionResult> SearchParties(
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] string? kind = null,
        [FromQuery] string? commercialStatus = null,
        [FromQuery] Guid? responsibleUserId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var (_, forbidden) = await AuthorizeAsync(Permissions.PartiesRead, resourceId: null, cancellationToken);

        if (forbidden is not null)
        {
            return forbidden;
        }

        if (page < 1 || pageSize is < 1 or > 100)
        {
            throw new PartyDomainException(ErrorCodes.ValidationError, 400, "page debe ser >= 1 y pageSize estar entre 1 y 100.");
        }

        if (q is { Length: > 200 })
        {
            throw new PartyDomainException(ErrorCodes.ValidationError, 400, "q admite hasta 200 caracteres.");
        }

        PartyKind? parsedKind = null;
        if (!string.IsNullOrEmpty(kind))
        {
            parsedKind = PartyWire.TryParseKind(kind, out var k)
                ? k
                : throw new PartyDomainException(ErrorCodes.ValidationError, 400, $"kind debe ser {PartyKinds.LegalEntity} o {PartyKinds.NaturalPerson}.");
        }

        CommercialStatus? parsedStatus = null;
        if (!string.IsNullOrEmpty(commercialStatus))
        {
            parsedStatus = PartyWire.TryParseCommercialStatus(commercialStatus, out var s)
                ? s
                : throw new PartyDomainException(ErrorCodes.ValidationError, 400, $"commercialStatus debe ser uno de {string.Join(", ", CommercialStatuses.All)}.");
        }

        return Ok(await partyService.SearchAsync(new PartySearchCriteria(q, parsedKind, parsedStatus, responsibleUserId), page, pageSize, cancellationToken));
    }

    [HttpGet("parties/{id:guid}")]
    public async Task<IActionResult> GetPartyDetail(Guid id, CancellationToken cancellationToken)
    {
        var (_, forbidden) = await AuthorizeAsync(Permissions.PartiesRead, id, cancellationToken);

        return forbidden ?? Ok(await partyService.GetPartyDetailAsync(id, cancellationToken));
    }

    [HttpGet("companies/{id:guid}")]
    public async Task<IActionResult> GetCompanyDetail(Guid id, CancellationToken cancellationToken)
    {
        var (_, forbidden) = await AuthorizeAsync(Permissions.PartiesRead, id, cancellationToken);

        return forbidden ?? Ok(await partyService.GetCompanyDetailAsync(id, cancellationToken));
    }

    [HttpGet("contacts/{id:guid}")]
    public async Task<IActionResult> GetContactDetail(Guid id, CancellationToken cancellationToken)
    {
        var (_, forbidden) = await AuthorizeAsync(Permissions.PartiesRead, id, cancellationToken);

        return forbidden ?? Ok(await partyService.GetContactDetailAsync(id, cancellationToken));
    }

    // ---------- Commands ----------

    [HttpPost("companies")]
    public async Task<IActionResult> CreateCompany([FromBody] PartyDataRequest request, CancellationToken cancellationToken)
    {
        var (context, forbidden) = await AuthorizeAsync(Permissions.PartiesWrite, resourceId: null, cancellationToken);

        if (forbidden is not null)
        {
            return forbidden;
        }

        var detail = await partyService.CreateCompanyAsync(context!, request.ToInput(), cancellationToken);

        return Created($"/api/v1/companies/{detail.PartyId}", detail);
    }

    [HttpPut("companies/{id:guid}")]
    public async Task<IActionResult> UpdateCompany(Guid id, [FromBody] PartyDataRequest request, CancellationToken cancellationToken)
    {
        var (context, forbidden) = await AuthorizeAsync(Permissions.PartiesWrite, id, cancellationToken);

        return forbidden ?? Ok(await partyService.UpdateCompanyAsync(context!, id, request.ToInput(), cancellationToken));
    }

    [HttpPost("contacts")]
    public async Task<IActionResult> CreateContact([FromBody] PartyDataRequest request, CancellationToken cancellationToken)
    {
        var (context, forbidden) = await AuthorizeAsync(Permissions.PartiesWrite, resourceId: null, cancellationToken);

        if (forbidden is not null)
        {
            return forbidden;
        }

        var detail = await partyService.CreateContactAsync(context!, request.ToInput(), cancellationToken);

        return Created($"/api/v1/contacts/{detail.PartyId}", detail);
    }

    [HttpPut("contacts/{id:guid}")]
    public async Task<IActionResult> UpdateContact(Guid id, [FromBody] PartyDataRequest request, CancellationToken cancellationToken)
    {
        var (context, forbidden) = await AuthorizeAsync(Permissions.PartiesWrite, id, cancellationToken);

        return forbidden ?? Ok(await partyService.UpdateContactAsync(context!, id, request.ToInput(), cancellationToken));
    }

    [HttpPost("contacts/{id:guid}/relationships")]
    public async Task<IActionResult> RelateContactToCompany(Guid id, [FromBody] RelateContactRequest request, CancellationToken cancellationToken)
    {
        var (context, forbidden) = await AuthorizeAsync(Permissions.PartiesWrite, id, cancellationToken);

        if (forbidden is not null)
        {
            return forbidden;
        }

        var relationship = await partyService.RelateContactToCompanyAsync(
            context!,
            id,
            request.CompanyId,
            request.RelationshipType ?? RelationshipTypes.ContactOf,
            cancellationToken);

        return Created($"/api/v1/contacts/{id}", relationship);
    }

    [HttpPost("parties/{id:guid}/commercial-status")]
    public async Task<IActionResult> ChangeCommercialStatus(Guid id, [FromBody] ChangeCommercialStatusRequest request, CancellationToken cancellationToken)
    {
        var (context, forbidden) = await AuthorizeAsync(Permissions.PartiesChangeCommercialStatus, id, cancellationToken);

        return forbidden ?? Ok(await partyService.ChangeCommercialStatusAsync(context!, id, request.CommercialStatus, cancellationToken));
    }

    [HttpPost("parties/{id:guid}/responsible")]
    public async Task<IActionResult> AssignResponsible(Guid id, [FromBody] AssignResponsibleRequest request, CancellationToken cancellationToken)
    {
        var (context, forbidden) = await AuthorizeAsync(Permissions.PartiesAssignResponsible, id, cancellationToken);

        return forbidden ?? Ok(await partyService.AssignResponsibleAsync(context!, id, request.ResponsibleUserId, cancellationToken));
    }

    private async Task<(ExecutionContextV1? Context, IActionResult? Forbidden)> AuthorizeAsync(
        string permission,
        Guid? resourceId,
        CancellationToken cancellationToken)
    {
        var context = await executionContextProvider.GetAsync(cancellationToken);
        var decision = await authorizationPort.EvaluateAsync(context!.ActorId, permission, ResourceTypes.Party, resourceId, cancellationToken);

        return decision.Allowed
            ? (context, null)
            : (context, ProblemDetailsResults.Forbidden(decision.ReasonCode!, context.CorrelationId, Request.Path));
    }
}
