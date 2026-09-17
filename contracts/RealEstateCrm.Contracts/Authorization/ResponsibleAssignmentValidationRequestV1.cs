namespace RealEstateCrm.Contracts.Authorization;

/// <summary>
/// Body v1 de <c>POST /api/v1/assignments/validate</c> en access-service. Forma compartida entre
/// el endpoint (Api) y el adapter HTTP de <c>IResponsibleAssignmentValidationPort</c>
/// (Infrastructure). <see cref="ActorId"/> viaja explícito en el body (misma convención que
/// <see cref="AuthorizationEvaluationRequestV1"/>): es una llamada servicio-a-servicio, no
/// necesariamente atada a un JWT de usuario forwardeado.
/// </summary>
public sealed record ResponsibleAssignmentValidationRequestV1(
    Guid ActorId,
    string ResourceType,
    Guid ResourceId,
    Guid ResponsibleUserId);
