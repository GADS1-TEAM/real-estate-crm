namespace RealEstateCrm.Contracts.Users;

/// <summary>
/// Respuesta de <c>GET /api/v1/users/me</c> (access-service): el usuario de negocio del propio
/// token, sin exponer búsqueda por <c>sub</c> de terceros. Resuelve el desfase entre los dos
/// esquemas de id del sistema: <c>ExecutionContextV1.ActorId</c> es el <c>sub</c> de Keycloak,
/// mientras que <c>responsibleUserId</c> (validador de asignación, eventos) es el
/// <see cref="UserId"/> propio de access-service.
/// </summary>
/// <param name="UserId">Id propio de access-service (el mismo que devuelve <c>GET /api/v1/users</c>).</param>
/// <param name="Status">Mismo texto que <c>UserSummary.Status</c> (<c>Active</c>, <c>Pending</c>, <c>Inactive</c>). El endpoint solo responde 200 con <c>Active</c>; los otros estados salen como 403.</param>
/// <param name="RoleCode">Rol activo (<c>Administrador</c>, <c>Vendedor</c>, <c>Responsable Comercial</c>); <see langword="null"/> si el usuario está activo pero sin rol asignado.</param>
public sealed record UserSelfV1(Guid UserId, string Status, string? RoleCode);
