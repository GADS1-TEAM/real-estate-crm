using RealEstateCrm.Contracts.Users;

namespace RealEstateCrm.BuildingBlocks.Authorization;

/// <summary>
/// Puerto para resolver, contra <c>access-service</c> (<c>GET /api/v1/users/me</c>), el usuario de
/// negocio del actor autenticado: su <c>userId</c> propio, su estado y su rol.
/// </summary>
/// <remarks>
/// Existe porque <c>ExecutionContextV1.ActorId</c> es el <c>sub</c> de Keycloak mientras que
/// <c>responsibleUserId</c> es el <c>userId</c> de access-service. Lo usa el owner de un recurso
/// (ej. <c>party-service</c>) para evaluar la regla de propiedad (D2) y para fijar el responsable
/// inicial. Solo resuelve al propio actor: no ofrece búsqueda de terceros.
/// </remarks>
public interface IUserDirectoryPort
{
    /// <summary>Resuelve el usuario del actor <paramref name="actorId"/> (su <c>sub</c>).</summary>
    Task<UserSelfResult> GetSelfAsync(Guid actorId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Resultado de <see cref="IUserDirectoryPort.GetSelfAsync"/>: o bien el usuario ACTIVE, o bien el
/// motivo de denegación (<c>user_pending</c>/<c>user_inactive</c>, ver <c>DenyReasons</c>).
/// </summary>
public sealed record UserSelfResult(UserSelfV1? User, string? DenyReasonCode)
{
    public bool IsActive => User is not null;

    public static UserSelfResult Active(UserSelfV1 user) => new(user, null);

    public static UserSelfResult Denied(string reasonCode) => new(null, reasonCode);
}
