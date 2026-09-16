namespace RealEstateCrm.BuildingBlocks.Authentication;

/// <summary>
/// Puerto para resolver el usuario autenticado de la request/mensaje actual.
/// </summary>
/// <remarks>
/// El adapter real (Keycloak, vía JwtBearer) vive en
/// <c>RealEstateCrm.BuildingBlocks.Infrastructure</c>. Este puerto no sabe nada de OIDC,
/// tokens ni HTTP: eso es responsabilidad del adapter.
/// </remarks>
public interface IAuthenticationPort
{
    /// <summary>
    /// Resuelve el usuario autenticado actual, o <see langword="null"/> si no hay ninguno
    /// (request/mensaje anónimo).
    /// </summary>
    Task<AuthenticatedUser?> ResolveCurrentUserAsync(CancellationToken cancellationToken = default);
}
