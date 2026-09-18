using RealEstateCrm.BuildingBlocks.Authentication;
using RealEstateCrm.BuildingBlocks.Infrastructure.Observability;
using RealEstateCrm.Contracts.Context;

namespace PlatformConfigService.Api.ExecutionContextResolution;

/// <summary>
/// Resuelve el <see cref="ExecutionContextV1"/> de la request HTTP actual, a partir del JWT ya
/// validado (<see cref="IAuthenticationPort"/>).
/// </summary>
/// <remarks>
/// A diferencia de <c>access-service</c> (que auto-provisiona <c>UserAccount</c> PENDING en el
/// primer login, D3), platform-config-service no tiene datos propios de usuario: el estado
/// PENDING/INACTIVE del actor lo resuelve <c>access-service</c> del otro lado de
/// <c>IAuthorizationPort.EvaluateAsync</c> (D2). <see cref="ExecutionContextV1.Permissions"/>
/// queda vacío a propósito: los controllers de este servicio nunca lo leen para autorizar, piden
/// la decisión puntual vía <c>IAuthorizationPort</c> (mismo patrón que <c>UsersController</c> de
/// access-service).
/// </remarks>
public sealed class CurrentExecutionContextProvider(
    IAuthenticationPort authenticationPort,
    IHttpContextAccessor httpContextAccessor)
{
    /// <summary><see langword="null"/> si no hay un usuario autenticado en la request actual.</summary>
    public async Task<ExecutionContextV1?> GetAsync(CancellationToken cancellationToken = default)
    {
        var authenticatedUser = await authenticationPort.ResolveCurrentUserAsync(cancellationToken);

        if (authenticatedUser is null)
        {
            return null;
        }

        return new ExecutionContextV1(
            ActorId: authenticatedUser.UserId,
            DisplayName: authenticatedUser.DisplayName,
            Email: authenticatedUser.Email,
            Roles: authenticatedUser.Roles,
            Permissions: Array.Empty<string>(),
            CorrelationId: ResolveCorrelationId(),
            CausationId: null);
    }

    private Guid ResolveCorrelationId()
    {
        var headerValue = httpContextAccessor.HttpContext?.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        return Guid.TryParse(headerValue, out var correlationId) ? correlationId : Guid.NewGuid();
    }
}
