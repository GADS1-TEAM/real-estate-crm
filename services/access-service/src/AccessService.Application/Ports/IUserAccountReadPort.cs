using AccessService.Domain;
using RealEstateCrm.Contracts.Paging;

namespace AccessService.Application.Ports;

/// <summary>
/// Puerto de lectura específico de access-service, complementario a
/// <c>IRepository{UserAccount, Guid}</c> (que solo resuelve por <c>UserId</c>, su <c>_id</c> de
/// Mongo). Cubre las dos consultas que ese repositorio no ofrece: buscar por
/// <see cref="UserAccount.KeycloakSubject"/> (login/autorización) y listar paginado (GetUsers).
/// </summary>
public interface IUserAccountReadPort
{
    /// <summary>
    /// Resuelve el <see cref="UserAccount"/> vinculado a un <c>sub</c> de Keycloak. Usado para
    /// autorización (el actor de <c>ExecutionContextV1</c>/<c>IAuthorizationPort</c> siempre viaja
    /// como <c>sub</c>) y para el auto-provisioning de un login desconocido (D3).
    /// </summary>
    Task<UserAccount?> GetByKeycloakSubjectAsync(Guid keycloakSubject, CancellationToken cancellationToken = default);

    /// <summary>Listado paginado de usuarios (GetUsers), orden estable por <c>DisplayName</c>.</summary>
    Task<PagedResult<UserAccount>> SearchAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
