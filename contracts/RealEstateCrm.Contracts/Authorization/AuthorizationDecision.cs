namespace RealEstateCrm.Contracts.Authorization;

/// <summary>
/// Resultado de evaluar un permiso vía
/// <see cref="RealEstateCrm.BuildingBlocks.Authorization.IAuthorizationPort"/>.
/// </summary>
/// <param name="Allowed">Si el actor tiene el permiso solicitado.</param>
/// <param name="ReasonCode">
/// Motivo de la denegación (ver <see cref="DenyReasons"/>). <see langword="null"/> cuando
/// <paramref name="Allowed"/> es <see langword="true"/>.
/// </param>
public sealed record AuthorizationDecision(bool Allowed, string? ReasonCode)
{
    /// <summary>Decisión de permiso concedido.</summary>
    public static AuthorizationDecision Allow() => new(Allowed: true, ReasonCode: null);

    /// <summary>Decisión de permiso denegado, con el motivo (ver <see cref="DenyReasons"/>).</summary>
    public static AuthorizationDecision Deny(string reasonCode) => new(Allowed: false, reasonCode);
}
