namespace AccessService.Application.Users;

/// <summary>
/// DTO mínimo de usuario (V2-ACL-001, sección Interfaces): "userId, displayName, email, status y
/// roleCodes". <see cref="RoleCodes"/> tiene 0 o 1 elementos (un solo rol activo por usuario,
/// AUTHZ-001); queda como colección para no romper el nombre del campo tal como lo define la task.
/// </summary>
public sealed record UserSummary(
    Guid UserId,
    string DisplayName,
    string Email,
    string Status,
    IReadOnlyCollection<string> RoleCodes);
