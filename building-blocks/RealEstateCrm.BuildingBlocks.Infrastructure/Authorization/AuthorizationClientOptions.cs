namespace RealEstateCrm.BuildingBlocks.Infrastructure.Authorization;

/// <summary>
/// Opciones del cliente HTTP interno hacia <c>access-service</c>, compartidas por
/// <see cref="HttpAuthorizationPort"/> y <see cref="HttpResponsibleAssignmentValidationPort"/>.
/// </summary>
public sealed class AuthorizationClientOptions
{
    /// <summary>Base URL de access-service, ej. "http://localhost:5101".</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Duración de la caché en memoria de decisiones de autorización (D2, plan Wave 2 sección 5:
    /// "IMemoryCache corto (≤60 s)"). Implica que una desactivación de usuario o un cambio de rol
    /// puede tardar hasta este tiempo en reflejarse en los servicios que cachean la decisión.
    /// </summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromSeconds(60);
}
