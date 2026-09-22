using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OperationsBff.Api.Controllers;

/// <summary>
/// Endpoints de autenticación del BFF. El login redirige al IdP (Keycloak) vía OIDC,
/// y al volver el middleware guarda los tokens en la cookie de sesión HttpOnly.
/// El front nunca ve el access token (AUTH-001, V2-FND-002).
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    /// <summary>
    /// Inicia el flujo OIDC contra Keycloak. El front redirige acá; el middleware
    /// se encarga de la negociación y al volver escribe la cookie de sesión.
    /// </summary>
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? returnUrl = null)
    {
        var redirectUri = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;
        return Challenge(new AuthenticationProperties { RedirectUri = redirectUri }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Cierra la sesión: borra la cookie local y emite logout en el IdP.
    /// </summary>
    [Authorize]
    [HttpGet("logout")]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Retorna información del usuario autenticado (claims de la sesión cookie).
    /// El front llama acá para saber quién está logueado.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        return Ok(new
        {
            isAuthenticated = User.Identity?.IsAuthenticated ?? false,
            name = User.Identity?.Name,
            claims
        });
    }
}
