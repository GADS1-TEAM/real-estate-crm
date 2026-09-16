using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RealEstateCrm.BuildingBlocks.Authentication;
using RealEstateCrm.Contracts.Errors;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Authentication;

/// <summary>
/// Registro de autenticación OIDC contra Keycloak para los dos roles que necesita V2:
/// servicios backend (validan un Bearer token) y BFF (dueño de la sesión de login).
/// </summary>
public static class KeycloakAuthenticationServiceCollectionExtensions
{
    private const string DefaultConfigurationSectionName = "Keycloak";

    /// <summary>
    /// Para un servicio backend: valida el JWT emitido por Keycloak y expone
    /// <see cref="IAuthenticationPort"/> resuelto desde sus claims (AUTH-002).
    /// Un token inválido o ausente produce un <see cref="Contracts.Errors.ProblemDetailsV1"/>
    /// estable en vez de una respuesta vacía.
    /// </summary>
    public static IServiceCollection AddKeycloakJwtBearerAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSectionName = DefaultConfigurationSectionName)
    {
        var options = BindOptions(configuration, configurationSectionName);

        return services.AddKeycloakJwtBearerAuthentication(bearerOptions =>
        {
            bearerOptions.Authority = options.Authority;
            bearerOptions.RequireHttpsMetadata = options.RequireHttpsMetadata;
            bearerOptions.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = !string.IsNullOrEmpty(options.Audience),
                ValidAudience = options.Audience,
                NameClaimType = "preferred_username",
            };
        });
    }

    /// <summary>
    /// Igual que el overload basado en <see cref="IConfiguration"/>, pero permite configurar
    /// <see cref="JwtBearerOptions"/> directamente (ej. <see cref="TokenValidationParameters"/>
    /// con una clave simétrica de test, sin depender de un Authority real). Pensado para tests
    /// del pipeline de autenticación sin Keycloak corriendo.
    /// </summary>
    public static IServiceCollection AddKeycloakJwtBearerAuthentication(
        this IServiceCollection services,
        Action<JwtBearerOptions> configureBearerOptions)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IAuthenticationPort, ClaimsPrincipalAuthenticationPort>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(bearerOptions =>
            {
                // Sin este flag, JwtBearerHandler remapea claims cortos de OIDC (sub, email,
                // name) a URIs largas de ClaimTypes, así que ClaimsPrincipalAuthenticationPort
                // no los encontraría por su nombre original emitido por Keycloak.
                bearerOptions.MapInboundClaims = false;

                configureBearerOptions(bearerOptions);

                bearerOptions.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        return ProblemDetailsChallengeWriter.WriteAsync(
                            context.HttpContext,
                            StatusCodes.Status401Unauthorized,
                            ErrorCodes.Unauthorized,
                            "No se pudo validar el token.",
                            context.ErrorDescription);
                    },
                    OnForbidden = context => ProblemDetailsChallengeWriter.WriteAsync(
                        context.HttpContext,
                        StatusCodes.Status403Forbidden,
                        ErrorCodes.Forbidden,
                        "El token no tiene permiso para este recurso.",
                        detail: null),
                };
            });

        return services;
    }

    /// <summary>
    /// Para un BFF: cookie de sesión HttpOnly + login interactivo vía OpenID Connect contra
    /// Keycloak (AUTH-001). Decisión del equipo (V2-FND-002): el token nunca llega al front;
    /// el BFF lo guarda del lado servidor y el front solo ve la cookie de sesión.
    /// </summary>
    public static IServiceCollection AddKeycloakOpenIdConnectCookieAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSectionName = DefaultConfigurationSectionName)
    {
        var options = BindOptions(configuration, configurationSectionName);

        services.AddHttpContextAccessor();
        services.AddScoped<IAuthenticationPort, ClaimsPrincipalAuthenticationPort>();

        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(cookieOptions =>
            {
                cookieOptions.Cookie.HttpOnly = true;
                cookieOptions.Cookie.SameSite = SameSiteMode.Lax;
                cookieOptions.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            })
            .AddOpenIdConnect(oidcOptions =>
            {
                oidcOptions.Authority = options.Authority;
                oidcOptions.ClientId = options.Audience;
                oidcOptions.RequireHttpsMetadata = options.RequireHttpsMetadata;
                oidcOptions.ResponseType = "code";
                oidcOptions.SaveTokens = true;
                oidcOptions.GetClaimsFromUserInfoEndpoint = true;
            });

        return services;
    }

    private static KeycloakAuthenticationOptions BindOptions(IConfiguration configuration, string configurationSectionName)
    {
        var options = new KeycloakAuthenticationOptions();
        configuration.GetSection(configurationSectionName).Bind(options);
        return options;
    }
}
