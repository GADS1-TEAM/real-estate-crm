using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace OperationsBff.Api.Authentication;

/// <summary>
/// Los endpoints de screens/mutations conservan acceso anónimo en la POC local,
/// pero no pueden exponer datos compartidos al publicar el BFF.
/// </summary>
public sealed class RequireSessionOutsideDevelopmentFilter(IHostEnvironment environment) : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!environment.IsDevelopment() && context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
