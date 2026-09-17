using AccessService.Api.ErrorHandling;
using AccessService.Api.ExecutionContextResolution;
using AccessService.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateCrm.BuildingBlocks.Authorization;
using RealEstateCrm.Contracts.Authorization;

namespace AccessService.Api.Users;

/// <summary>
/// USR-001/USR-002/AUTHZ-001/AUTHZ-002 (V2-ACL-001). Controller delgado (aspnetcore-rest-layer
/// skill): valida en el borde, traduce la decisión de <see cref="IAuthorizationPort"/> a 403/200
/// y delega toda la lógica de negocio en <see cref="UserAccountService"/>.
/// </summary>
[ApiController]
[Route("api/v1/users")]
[Authorize]
public sealed class UsersController(
    UserAccountService userAccountService,
    IAuthorizationPort authorizationPort,
    CurrentExecutionContextProvider executionContextProvider) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var context = await executionContextProvider.GetAsync(cancellationToken);
        var decision = await authorizationPort.EvaluateAsync(context!.ActorId, Permissions.UsersManage, ResourceTypes.User, resourceId: null, cancellationToken);

        if (!decision.Allowed)
        {
            return ProblemDetailsResults.Forbidden(decision.ReasonCode!, context.CorrelationId, Request.Path);
        }

        var summary = await userAccountService.CreateUserAsync(context, request.KeycloakSubject, request.DisplayName, request.Email, cancellationToken);

        return Created($"/api/v1/users/{summary.UserId}", summary);
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var context = await executionContextProvider.GetAsync(cancellationToken);
        var decision = await authorizationPort.EvaluateAsync(context!.ActorId, Permissions.UsersRead, ResourceTypes.User, resourceId: null, cancellationToken);

        if (!decision.Allowed)
        {
            return ProblemDetailsResults.Forbidden(decision.ReasonCode!, context.CorrelationId, Request.Path);
        }

        var pageResult = await userAccountService.GetUsersAsync(page, pageSize, cancellationToken);

        return Ok(pageResult);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var context = await executionContextProvider.GetAsync(cancellationToken);
        var decision = await authorizationPort.EvaluateAsync(context!.ActorId, Permissions.UsersManage, ResourceTypes.User, id, cancellationToken);

        if (!decision.Allowed)
        {
            return ProblemDetailsResults.Forbidden(decision.ReasonCode!, context.CorrelationId, Request.Path);
        }

        var summary = await userAccountService.UpdateUserAsync(context, id, request.DisplayName, request.Email, cancellationToken);

        return Ok(summary);
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateUser(Guid id, CancellationToken cancellationToken)
    {
        var context = await executionContextProvider.GetAsync(cancellationToken);
        var decision = await authorizationPort.EvaluateAsync(context!.ActorId, Permissions.UsersManage, ResourceTypes.User, id, cancellationToken);

        if (!decision.Allowed)
        {
            return ProblemDetailsResults.Forbidden(decision.ReasonCode!, context.CorrelationId, Request.Path);
        }

        var summary = await userAccountService.DeactivateUserAsync(context, id, cancellationToken);

        return Ok(summary);
    }

    [HttpPost("{id:guid}/role-assignment")]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] AssignRoleRequest request, CancellationToken cancellationToken)
    {
        var context = await executionContextProvider.GetAsync(cancellationToken);
        var decision = await authorizationPort.EvaluateAsync(context!.ActorId, Permissions.UsersManage, ResourceTypes.User, id, cancellationToken);

        if (!decision.Allowed)
        {
            return ProblemDetailsResults.Forbidden(decision.ReasonCode!, context.CorrelationId, Request.Path);
        }

        var summary = await userAccountService.AssignRoleAsync(context, id, request.RoleCode, cancellationToken);

        return Ok(summary);
    }

    [HttpGet("{id:guid}/effective-permissions")]
    public async Task<IActionResult> GetEffectivePermissions(Guid id, CancellationToken cancellationToken)
    {
        var context = await executionContextProvider.GetAsync(cancellationToken);
        var decision = await authorizationPort.EvaluateAsync(context!.ActorId, Permissions.UsersRead, ResourceTypes.User, id, cancellationToken);

        if (!decision.Allowed)
        {
            return ProblemDetailsResults.Forbidden(decision.ReasonCode!, context.CorrelationId, Request.Path);
        }

        var result = await userAccountService.GetEffectivePermissionsAsync(id, cancellationToken);

        return Ok(result);
    }
}
