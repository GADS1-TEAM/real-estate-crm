using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using OperationsBff.Api.Authentication;
using System.Security.Claims;

public sealed class HostedAuthenticationFilterTests
{
    [Fact]
    public void Hosted_screen_without_session_requires_cookie_login()
    {
        var context = CreateContext();
        new RequireSessionOutsideDevelopmentFilter(new TestEnvironment("Production")).OnAuthorization(context);

        var challenge = Assert.IsType<ChallengeResult>(context.Result);
        Assert.Contains(CookieAuthenticationDefaults.AuthenticationScheme, challenge.AuthenticationSchemes);
    }

    [Fact]
    public void Hosted_screen_with_session_is_allowed()
    {
        var context = CreateContext();
        context.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user")], "Cookies"));

        new RequireSessionOutsideDevelopmentFilter(new TestEnvironment("Production")).OnAuthorization(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void Local_development_keeps_the_existing_anonymous_flow()
    {
        var context = CreateContext();
        new RequireSessionOutsideDevelopmentFilter(new TestEnvironment("Development")).OnAuthorization(context);
        Assert.Null(context.Result);
    }

    private static AuthorizationFilterContext CreateContext() => new(
        new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()), []);

    private sealed class TestEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
