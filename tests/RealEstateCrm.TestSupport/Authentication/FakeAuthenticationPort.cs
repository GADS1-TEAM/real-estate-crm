using RealEstateCrm.BuildingBlocks.Authentication;

namespace RealEstateCrm.TestSupport.Authentication;

/// <summary>
/// Implementación en memoria de <see cref="IAuthenticationPort"/> para unit tests.
/// Habilitada explícitamente para la POC (V2-FND-002, "Overrides POC").
/// </summary>
/// <remarks>
/// Vive en un proyecto de test support, no en <c>RealEstateCrm.BuildingBlocks</c>: es un
/// double de test, no un puerto ni un contrato de producción, así que ningún servicio real
/// debería poder referenciarlo.
/// </remarks>
public sealed class FakeAuthenticationPort : IAuthenticationPort
{
    private readonly AuthenticatedUser? _user;

    public FakeAuthenticationPort(AuthenticatedUser? user)
    {
        _user = user;
    }

    /// <summary>Crea un fake que siempre resuelve un usuario de desarrollo con los roles indicados.</summary>
    public static FakeAuthenticationPort ForUser(string displayName = "Dev User", string email = "dev@local.test", params string[] roles) =>
        new(new AuthenticatedUser(Guid.NewGuid(), displayName, email, roles));

    /// <summary>Crea un fake que siempre resuelve request/mensaje anónimo.</summary>
    public static FakeAuthenticationPort Anonymous() => new(user: null);

    public Task<AuthenticatedUser?> ResolveCurrentUserAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_user);
}
