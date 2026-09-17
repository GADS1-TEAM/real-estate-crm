using RealEstateCrm.BuildingBlocks.Authorization;
using RealEstateCrm.Contracts.Authorization;

namespace RealEstateCrm.TestSupport.Authorization;

/// <summary>
/// Implementación en memoria de <see cref="IResponsibleAssignmentValidationPort"/> para unit
/// tests de servicios owner (ej. V2-PTY-001), hasta que exista un cliente HTTP real registrado.
/// </summary>
/// <remarks>
/// Vive en un proyecto de test support, no en <c>RealEstateCrm.BuildingBlocks</c>: es un double
/// de test, no un puerto ni un contrato de producción.
/// </remarks>
public sealed class FakeResponsibleAssignmentValidationPort : IResponsibleAssignmentValidationPort
{
    private readonly Func<Guid, string, Guid, Guid, AuthorizationDecision> _validate;

    public FakeResponsibleAssignmentValidationPort(Func<Guid, string, Guid, Guid, AuthorizationDecision> validate)
    {
        _validate = validate;
    }

    /// <summary>Crea un fake que aprueba cualquier asignación de responsable.</summary>
    public static FakeResponsibleAssignmentValidationPort AllowAll() =>
        new((_, _, _, _) => AuthorizationDecision.Allow());

    /// <summary>Crea un fake que rechaza cualquier asignación de responsable con el motivo indicado.</summary>
    public static FakeResponsibleAssignmentValidationPort DenyAll(
        string reasonCode = ResponsibleAssignmentDenyReasons.PermissionNotGranted) =>
        new((_, _, _, _) => AuthorizationDecision.Deny(reasonCode));

    public Task<AuthorizationDecision> ValidateAsync(
        Guid actorId,
        string resourceType,
        Guid resourceId,
        Guid responsibleUserId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_validate(actorId, resourceType, resourceId, responsibleUserId));
}
