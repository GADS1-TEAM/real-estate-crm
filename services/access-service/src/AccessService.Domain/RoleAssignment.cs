namespace AccessService.Domain;

/// <summary>
/// Aggregate root del rol activo de un usuario (AUTHZ-001). Un solo rol activo por usuario:
/// el <see cref="UserId"/> ES el id del documento (invariante enforzada por construcción, no por
/// validación de negocio), así que asignar un rol nuevo reemplaza al anterior, nunca lo apila.
/// </summary>
/// <remarks>
/// Inmutable (record), mismo patrón que <see cref="UserAccount"/>.
/// </remarks>
public sealed record RoleAssignment(
    Guid UserId,
    string RoleCode,
    Guid AssignedByUserId,
    DateTimeOffset AssignedAt,
    int Version)
{
    /// <summary>Primera asignación de rol de un usuario.</summary>
    public static RoleAssignment Create(Guid userId, string roleCode, Guid assignedByUserId, DateTimeOffset assignedAt) =>
        new(userId, roleCode, assignedByUserId, assignedAt, Version: 1);

    /// <summary>Reemplaza el rol activo por uno nuevo (AUTHZ-001: "asignar UNO de los tres roles").</summary>
    public RoleAssignment Reassign(string newRoleCode, Guid assignedByUserId, DateTimeOffset assignedAt) =>
        this with { RoleCode = newRoleCode, AssignedByUserId = assignedByUserId, AssignedAt = assignedAt, Version = Version + 1 };
}
