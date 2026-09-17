using RealEstateCrm.BuildingBlocks.Authorization;
using RealEstateCrm.Contracts.Authorization;

namespace RealEstateCrm.TestSupport.Authorization;

/// <summary>
/// Implementación en memoria de <see cref="IAuthorizationPort"/> para unit tests de servicios
/// owner (ej. CAT-001, PTY-001), hasta que exista el adapter HTTP real de V2-ACL-001.
/// </summary>
/// <remarks>
/// Vive en un proyecto de test support, no en <c>RealEstateCrm.BuildingBlocks</c>: es un double
/// de test, no un puerto ni un contrato de producción.
/// </remarks>
public sealed class FakeAuthorizationPort : IAuthorizationPort
{
    private readonly Func<Guid, string, string, Guid?, AuthorizationDecision> _evaluate;

    public FakeAuthorizationPort(Func<Guid, string, string, Guid?, AuthorizationDecision> evaluate)
    {
        _evaluate = evaluate;
    }

    /// <summary>Crea un fake que concede cualquier permiso.</summary>
    public static FakeAuthorizationPort AllowAll() =>
        new((_, _, _, _) => AuthorizationDecision.Allow());

    /// <summary>Crea un fake que deniega cualquier permiso con el motivo indicado.</summary>
    public static FakeAuthorizationPort DenyAll(string reasonCode = DenyReasons.PermissionNotGranted) =>
        new((_, _, _, _) => AuthorizationDecision.Deny(reasonCode));

    /// <summary>
    /// Crea un fake que solo concede los permisos indicados; el resto se deniega con
    /// <see cref="DenyReasons.PermissionNotGranted"/>.
    /// </summary>
    public static FakeAuthorizationPort AllowingOnly(params string[] permissions) =>
        new((_, permission, _, _) => permissions.Contains(permission, StringComparer.Ordinal)
            ? AuthorizationDecision.Allow()
            : AuthorizationDecision.Deny(DenyReasons.PermissionNotGranted));

    public Task<AuthorizationDecision> EvaluateAsync(
        Guid actorId,
        string permission,
        string resourceType,
        Guid? resourceId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_evaluate(actorId, permission, resourceType, resourceId));
}
