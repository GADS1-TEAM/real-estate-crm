using RealEstateCrm.Contracts.Authorization;

namespace RealEstateCrm.BuildingBlocks.Authorization;

/// <summary>
/// Puerto para validar, contra <c>access-service</c>, una asignación o reasignación de
/// responsable comercial sobre un recurso de negocio (ASSIGN-001/ASSIGN-002, V2-ACL-001).
/// </summary>
/// <remarks>
/// Solo valida: no persiste la asignación ni decide qué campo del recurso se actualiza. El dato
/// <c>responsibleUserId</c> y el evento de dominio correspondiente (ej. "PartyResponsibleAssigned")
/// son responsabilidad del servicio owner del recurso (ej. <c>party-service</c>, V2-PTY-001), que
/// llama a este puerto ANTES de mutar su propio aggregate, y recién después persiste el cambio y
/// publica su evento usando la forma de payload publicada en
/// <c>RealEstateCrm.Contracts.Events.Access.ResponsibleAssignedV1</c>.
/// <para>
/// El adapter real (HTTP a <c>access-service</c>, <c>POST /api/v1/assignments/validate</c>) vive
/// en <c>RealEstateCrm.BuildingBlocks.Infrastructure</c>.
/// </para>
/// </remarks>
public interface IResponsibleAssignmentValidationPort
{
    /// <summary>
    /// Evalúa si <paramref name="actorId"/> puede asignar a <paramref name="responsibleUserId"/>
    /// como responsable de un recurso <paramref name="resourceType"/>/<paramref name="resourceId"/>.
    /// Ver <see cref="Contracts.Authorization.ResponsibleAssignmentDenyReasons"/> para los motivos
    /// de denegación posibles.
    /// </summary>
    Task<AuthorizationDecision> ValidateAsync(
        Guid actorId,
        string resourceType,
        Guid resourceId,
        Guid responsibleUserId,
        CancellationToken cancellationToken = default);
}
