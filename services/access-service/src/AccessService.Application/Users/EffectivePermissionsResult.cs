namespace AccessService.Application.Users;

/// <summary>Resultado de GetEffectivePermissions (AUTHZ-002).</summary>
public sealed record EffectivePermissionsResult(
    Guid UserId,
    string? RoleCode,
    IReadOnlyCollection<string> Permissions);
