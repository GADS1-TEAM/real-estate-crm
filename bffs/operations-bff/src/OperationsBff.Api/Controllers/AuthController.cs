using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OperationsBff.Api.Controllers;

/// <summary>
/// Endpoints de autenticación del BFF. Soporta tanto redirección OIDC contra Keycloak
/// como inicio de sesión directo de las cuentas configuradas, guardando siempre la sesión
/// en cookie HttpOnly del lado servidor.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[AllowAnonymous]
public sealed class AuthController(IConfiguration configuration, IHttpClientFactory httpClientFactory) : ControllerBase
{
    public sealed record DirectLoginRequest(string? Username, string? Password, string? Account);

    [HttpGet("login")]
    public async Task<IActionResult> Login([FromQuery] string? account = null, [FromQuery] string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var frontendBase = "http://localhost:3000";
        var targetUrl = string.IsNullOrWhiteSpace(returnUrl) ? $"{frontendBase}/inicio" : returnUrl;

        if (!string.IsNullOrWhiteSpace(account))
        {
            var resolved = ResolveAccount(account, null);
            await SignInAccountAsync(resolved.Username, resolved.Password, resolved.Name, resolved.Email, resolved.RoleId, resolved.KeycloakRole, cancellationToken);
            var separator = targetUrl.Contains('?') ? "&" : "?";
            return Redirect($"{targetUrl}{separator}auth_role={Uri.EscapeDataString(resolved.RoleId)}");
        }

        var authority = configuration["Keycloak:Authority"]?.TrimEnd('/') ?? "http://localhost:8080/realms/crm-dev";
        var clientId = configuration["Keycloak:Audience"] ?? "operations-bff";
        var callbackUrl = $"{Request.Scheme}://{Request.Host}/api/v1/auth/callback";
        var authorizeUrl = $"{authority}/protocol/openid-connect/auth"
            + $"?client_id={Uri.EscapeDataString(clientId)}"
            + $"&response_type=code"
            + $"&scope={Uri.EscapeDataString("openid profile email")}"
            + $"&redirect_uri={Uri.EscapeDataString(callbackUrl)}"
            + $"&state={Uri.EscapeDataString(targetUrl)}";

        return Redirect(authorizeUrl);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string? code = null, [FromQuery] string? state = null, CancellationToken cancellationToken = default)
    {
        var targetUrl = string.IsNullOrWhiteSpace(state) ? "http://localhost:3000/inicio" : state;
        if (string.IsNullOrWhiteSpace(code))
        {
            return Redirect(targetUrl);
        }

        var authority = configuration["Keycloak:Authority"]?.TrimEnd('/') ?? "http://localhost:8080/realms/crm-dev";
        var clientId = configuration["Keycloak:Audience"] ?? "operations-bff";
        var callbackUrl = $"{Request.Scheme}://{Request.Host}/api/v1/auth/callback";

        string roleId = "vendedor";
        string displayName = "Martín Quiroga";
        string email = "martin@inmobiliaria.com.ar";
        string? accessToken = null;

        try
        {
            using var client = httpClientFactory.CreateClient();
            var tokenResponse = await client.PostAsync(
                $"{authority}/protocol/openid-connect/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "authorization_code",
                    ["client_id"] = clientId,
                    ["code"] = code,
                    ["redirect_uri"] = callbackUrl
                }),
                cancellationToken);

            if (tokenResponse.IsSuccessStatusCode)
            {
                var json = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("access_token", out var at))
                {
                    accessToken = at.GetString();
                    var claimsInfo = ParseJwtClaims(accessToken);
                    roleId = claimsInfo.RoleId;
                    displayName = claimsInfo.Name;
                    email = claimsInfo.Email;
                }
            }
        }
        catch
        {
            // Fallback to default session if Keycloak token exchange fails
        }

        await IssueSessionCookieAsync(displayName, email, roleId, accessToken);
        var separator = targetUrl.Contains('?') ? "&" : "?";
        return Redirect($"{targetUrl}{separator}auth_role={Uri.EscapeDataString(roleId)}");
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginDirect([FromBody] DirectLoginRequest request, CancellationToken cancellationToken = default)
    {
        var identifier = (request.Account ?? request.Username ?? "").Trim();
        var password = (request.Password ?? "").Trim();

        var resolved = ResolveAccount(identifier, password);
        if (!resolved.Valid)
        {
            return Unauthorized(new
            {
                authenticated = false,
                message = "Credenciales inválidas. Usá martin@inmobiliaria.com.ar (clave: martin123) o rodrigo@inmobiliaria.com.ar (clave: rodrigo123)."
            });
        }

        await SignInAccountAsync(resolved.Username, resolved.Password, resolved.Name, resolved.Email, resolved.RoleId, resolved.KeycloakRole, cancellationToken);

        return Ok(new
        {
            authenticated = true,
            name = resolved.Name,
            email = resolved.Email,
            roleId = resolved.RoleId,
            roleLabel = resolved.RoleLabel
        });
    }

    [HttpGet("logout")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromQuery] string? returnUrl = null)
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var target = string.IsNullOrWhiteSpace(returnUrl) ? "http://localhost:3000/login" : returnUrl;
        if (Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new { loggedOut = true });
        }
        return Redirect(target);
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
        var name = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("name") ?? "Martín Quiroga";
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? "martin@inmobiliaria.com.ar";
        var roleId = User.FindFirstValue("crm_role_id") ?? "vendedor";
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        return Ok(new
        {
            isAuthenticated,
            name,
            email,
            roleId,
            claims
        });
    }

    private async Task SignInAccountAsync(string username, string password, string name, string email, string roleId, string keycloakRole, CancellationToken cancellationToken)
    {
        string? accessToken = null;
        try
        {
            var authority = configuration["Keycloak:Authority"]?.TrimEnd('/') ?? "http://localhost:8080/realms/crm-dev";
            var clientId = configuration["Keycloak:Audience"] ?? "operations-bff";
            using var client = httpClientFactory.CreateClient();
            var tokenResponse = await client.PostAsync(
                $"{authority}/protocol/openid-connect/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "password",
                    ["client_id"] = clientId,
                    ["username"] = username,
                    ["password"] = password
                }),
                cancellationToken);

            if (tokenResponse.IsSuccessStatusCode)
            {
                var json = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("access_token", out var at))
                {
                    accessToken = at.GetString();
                }
            }
        }
        catch
        {
            // Proceed with local session cookie
        }

        await IssueSessionCookieAsync(name, email, roleId, accessToken);
    }

    private async Task IssueSessionCookieAsync(string name, string email, string roleId, string? accessToken)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, name),
            new("name", name),
            new(ClaimTypes.Email, email),
            new("email", email),
            new("crm_role_id", roleId)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var props = new AuthenticationProperties { IsPersistent = true };
        var tokens = new List<AuthenticationToken>
        {
            new() { Name = "access_token", Value = "dev-poc-token" }
        };
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            tokens.Add(new AuthenticationToken { Name = "id_token", Value = accessToken });
        }
        props.StoreTokens(tokens);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
    }

    private static (bool Valid, string Username, string Password, string Name, string Email, string RoleId, string RoleLabel, string KeycloakRole) ResolveAccount(string identifier, string? password)
    {
        var norm = identifier.Trim().ToLowerInvariant();
        var pass = password?.Trim() ?? "";
        if (string.IsNullOrEmpty(norm) || string.IsNullOrEmpty(pass))
        {
            return (false, "", "", "", "", "vendedor", "Vendedor", "Vendedor");
        }

        if (norm is "rodrigo.vergara" or "rodrigo@inmobiliaria.com.ar" or "dev.responsable" or "dev.responsable@crm-dev.local")
        {
            var validPass = pass is "rodrigo123" or "dev.responsable";
            return (validPass, "rodrigo.vergara", "rodrigo123", "Rodrigo Vergara", "rodrigo@inmobiliaria.com.ar", "responsable", "Responsable comercial", "Responsable Comercial");
        }

        if (norm is "sofia.rendon" or "sofia@inmobiliaria.com.ar" or "dev.administrador" or "dev.administrador@crm-dev.local")
        {
            var validPass = pass is "sofia123" or "dev.administrador";
            return (validPass, "dev.administrador", "dev.administrador", "Sofía Rendón", "sofia@inmobiliaria.com.ar", "administradora", "Administradora", "Administrador");
        }

        if (norm is "lucia.ferrari" or "lucia@inmobiliaria.com.ar")
        {
            var validPass = pass is "lucia123";
            return (validPass, "martin.quiroga", "martin123", "Lucía Ferrari", "lucia@inmobiliaria.com.ar", "vendedor", "Vendedor", "Vendedor");
        }

        if (norm is "martin.quiroga" or "martin@inmobiliaria.com.ar" or "dev.vendedor" or "dev.vendedor@crm-dev.local")
        {
            var validPass = pass is "martin123" or "dev.vendedor";
            return (validPass, "martin.quiroga", "martin123", "Martín Quiroga", "martin@inmobiliaria.com.ar", "vendedor", "Vendedor", "Vendedor");
        }

        return (false, "", "", "", "", "vendedor", "Vendedor", "Vendedor");
    }

    private static (string RoleId, string Name, string Email) ParseJwtClaims(string? jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
            return ("vendedor", "Martín Quiroga", "martin@inmobiliaria.com.ar");

        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2)
                return ("vendedor", "Martín Quiroga", "martin@inmobiliaria.com.ar");

            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var username = root.TryGetProperty("preferred_username", out var pu) ? pu.GetString() ?? "" : "";
            var email = root.TryGetProperty("email", out var em) ? em.GetString() ?? "" : "";
            var name = root.TryGetProperty("name", out var nm) ? nm.GetString() ?? "" : "";

            if (username.Contains("rodrigo", StringComparison.OrdinalIgnoreCase) || username.Contains("responsable", StringComparison.OrdinalIgnoreCase) || json.Contains("Responsable Comercial", StringComparison.OrdinalIgnoreCase))
            {
                return ("responsable", string.IsNullOrWhiteSpace(name) ? "Rodrigo Vergara" : name, string.IsNullOrWhiteSpace(email) ? "rodrigo@inmobiliaria.com.ar" : email);
            }

            if (username.Contains("admin", StringComparison.OrdinalIgnoreCase) || json.Contains("Administrador", StringComparison.OrdinalIgnoreCase))
            {
                return ("administradora", string.IsNullOrWhiteSpace(name) ? "Sofía Rendón" : name, string.IsNullOrWhiteSpace(email) ? "sofia@inmobiliaria.com.ar" : email);
            }

            return ("vendedor", string.IsNullOrWhiteSpace(name) ? "Martín Quiroga" : name, string.IsNullOrWhiteSpace(email) ? "martin@inmobiliaria.com.ar" : email);
        }
        catch
        {
            return ("vendedor", "Martín Quiroga", "martin@inmobiliaria.com.ar");
        }
    }
}
