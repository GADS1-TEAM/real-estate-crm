namespace RealEstateCrm.Contracts.Authorization;

/// <summary>
/// Body v1 de <c>POST /api/v1/authorization/evaluate</c> en access-service. Forma compartida
/// entre el endpoint (Api) y el adapter HTTP de <c>IAuthorizationPort</c> (Infrastructure).
/// </summary>
public sealed record AuthorizationEvaluationRequestV1(
    Guid ActorId,
    string Permission,
    string ResourceType,
    Guid? ResourceId);
