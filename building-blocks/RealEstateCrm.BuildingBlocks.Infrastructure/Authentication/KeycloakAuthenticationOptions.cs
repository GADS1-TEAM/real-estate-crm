namespace RealEstateCrm.BuildingBlocks.Infrastructure.Authentication;

/// <summary>
/// Opciones de configuración del realm de Keycloak usado para validar tokens OIDC.
/// Se bindean desde la sección de configuración "Keycloak" de cada Api.
/// </summary>
public sealed class KeycloakAuthenticationOptions
{
    /// <summary>URL del realm, ej. "http://localhost:8080/realms/crm-dev". Keycloak expone el resto de los endpoints OIDC vía metadata a partir de esta URL.</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>Audience esperada en el token (client id del recurso), o vacío para no validarla.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Exigir HTTPS en el metadata endpoint. Solo se apaga para el realm local de desarrollo (V2-FND-003).</summary>
    public bool RequireHttpsMetadata { get; set; } = true;
}
