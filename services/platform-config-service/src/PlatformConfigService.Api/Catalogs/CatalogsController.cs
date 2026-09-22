using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformConfigService.Api.ErrorHandling;
using PlatformConfigService.Api.ExecutionContextResolution;
using PlatformConfigService.Application.Catalogs;
using RealEstateCrm.BuildingBlocks.Authorization;
using RealEstateCrm.Contracts.Authorization;
using RealEstateCrm.Contracts.Context;
using RealEstateCrm.Contracts.Events.PlatformConfig;

namespace PlatformConfigService.Api.Catalogs;

/// <summary>
/// CAT-001..CAT-006 (V2-CAT-001). Controller delgado (aspnetcore-rest-layer skill): valida en el
/// borde, traduce la decisión de <see cref="IAuthorizationPort"/> a 403/200 y delega toda la
/// lógica de negocio en <see cref="CatalogService"/>. Autorización: todo autenticado lee
/// (<c>catalogs.read</c>), solo Administrador gestiona (<c>catalogs.manage</c>).
/// </summary>
[ApiController]
[Route("api/v1/catalogs")]
public sealed class CatalogsController(
    CatalogService catalogService,
    IAuthorizationPort authorizationPort,
    CurrentExecutionContextProvider executionContextProvider) : ControllerBase
{
    [HttpGet("{catalogType}")]
    public async Task<IActionResult> GetCatalog(
        string catalogType,
        CancellationToken cancellationToken,
        [FromQuery] bool activeOnly = false,
        [FromQuery] int? version = null)
    {
        var (context, forbidden) = await AuthorizeAsync(Permissions.CatalogsRead, resourceId: null, cancellationToken);
        if (forbidden is not null) return forbidden;

        var result = await catalogService.GetCatalogAsync(catalogType, activeOnly, version, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCatalogEntry([FromBody] CreateCatalogEntryRequest request, CancellationToken cancellationToken)
    {
        var (context, forbidden) = await AuthorizeAsync(Permissions.CatalogsManage, resourceId: null, cancellationToken);
        if (forbidden is not null) return forbidden;

        var entry = await catalogService.CreateCatalogEntryAsync(
            context!, request.CatalogType, request.Code, request.Label, request.Order, request.PipelineKind, request.SemanticState, cancellationToken);

        return Created($"/api/v1/catalogs/{entry.CatalogType}", entry);
    }

    [HttpPut("{entryId:guid}")]
    public async Task<IActionResult> UpdateCatalogEntry(Guid entryId, [FromBody] UpdateCatalogEntryRequest request, CancellationToken cancellationToken)
    {
        var (context, forbidden) = await AuthorizeAsync(Permissions.CatalogsManage, resourceId: entryId, cancellationToken);
        if (forbidden is not null) return forbidden;

        var entry = await catalogService.UpdateCatalogEntryAsync(context!, entryId, request.Label, request.Order, cancellationToken);
        return Ok(entry);
    }

    [HttpPost("{entryId:guid}/deactivate")]
    public async Task<IActionResult> DeactivateCatalogEntry(Guid entryId, CancellationToken cancellationToken)
    {
        var (context, forbidden) = await AuthorizeAsync(Permissions.CatalogsManage, resourceId: entryId, cancellationToken);
        if (forbidden is not null) return forbidden;

        var entry = await catalogService.DeactivateCatalogEntryAsync(context!, entryId, cancellationToken);
        return Ok(entry);
    }

    [HttpPost("{catalogType}/publish")]
    public async Task<IActionResult> PublishCatalogVersion(string catalogType, CancellationToken cancellationToken)
    {
        var (context, forbidden) = await AuthorizeAsync(Permissions.CatalogsManage, resourceId: null, cancellationToken);
        if (forbidden is not null) return forbidden;

        var newVersion = await catalogService.PublishCatalogVersionAsync(context!, catalogType, cancellationToken);
        return Ok(new CatalogVersionPublishedV1(catalogType, newVersion));
    }

    private async Task<(ExecutionContextV1? Context, IActionResult? Forbidden)> AuthorizeAsync(
        string permission,
        Guid? resourceId,
        CancellationToken cancellationToken)
    {
        var context = await executionContextProvider.GetAsync(cancellationToken);
        if (context is null)
        {
            return (null, ProblemDetailsResults.Unauthorized("Token ausente o inválido.", Guid.NewGuid(), Request.Path));
        }

        var decision = await authorizationPort.EvaluateAsync(context.ActorId, permission, ResourceTypes.Catalog, resourceId, cancellationToken);

        return decision.Allowed
            ? (context, null)
            : (context, ProblemDetailsResults.Forbidden(decision.ReasonCode!, context.CorrelationId, Request.Path));
    }
}
