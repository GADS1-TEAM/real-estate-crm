namespace RealEstateCrm.Contracts.Events.Access;

/// <summary>
/// Payload v1 de <see cref="EventEnvelopeV1{TPayload}"/> para el evento "ResponsibleAssigned".
/// </summary>
/// <remarks>
/// Publicado por el servicio OWNER del recurso (ej. <c>party-service</c> en V2-PTY-001), no por
/// <c>access-service</c>: <c>responsibleUserId</c> es dato del owner, access-service solo valida
/// la asignación (<c>POST /api/v1/assignments/validate</c>, puerto
/// <c>IResponsibleAssignmentValidationPort</c>). Este contrato se publica desde V2-ACL-001 para
/// que V2-PTY-001 lo consuma sin tener que redefinirlo.
/// </remarks>
/// <param name="ResourceType">Ver <see cref="Authorization.ResourceTypes"/>.</param>
/// <param name="PreviousResponsibleUserId"><see langword="null"/> en la primera asignación.</param>
public sealed record ResponsibleAssignedV1(
    string ResourceType,
    Guid ResourceId,
    Guid? PreviousResponsibleUserId,
    Guid NewResponsibleUserId,
    Guid AssignedByUserId);
