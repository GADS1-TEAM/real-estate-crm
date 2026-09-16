namespace RealEstateCrm.BuildingBlocks.Authentication;

/// <summary>
/// Usuario resuelto por el identity provider (Keycloak) para la request/mensaje actual.
/// </summary>
/// <remarks>
/// Solo identidad: no incluye permisos de negocio. Esos los resuelve access-service
/// (AGENTS.md, "Autenticación vs autorización"; ARCHITECTURE.md §5). Un token válido no
/// equivale automáticamente a permiso para toda operación.
/// </remarks>
/// <param name="UserId">Identificador estable del usuario (claim "sub").</param>
/// <param name="DisplayName">Nombre para mostrar.</param>
/// <param name="Email">Email del usuario.</param>
/// <param name="Roles">Roles tal como los emite el identity provider (sin resolver permisos).</param>
public sealed record AuthenticatedUser(
    Guid UserId,
    string DisplayName,
    string Email,
    IReadOnlyCollection<string> Roles);
