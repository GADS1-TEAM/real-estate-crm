namespace RealEstateCrm.Contracts.Events.Access;

/// <summary>Payload v1 de <see cref="EventEnvelopeV1{TPayload}"/> para el evento "UserCreated" publicado por access-service.</summary>
public sealed record UserCreatedV1(Guid UserId, string DisplayName, string Email, string Status);

/// <summary>Payload v1 de <see cref="EventEnvelopeV1{TPayload}"/> para el evento "UserUpdated" publicado por access-service.</summary>
public sealed record UserUpdatedV1(Guid UserId, string DisplayName, string Email);

/// <summary>Payload v1 de <see cref="EventEnvelopeV1{TPayload}"/> para el evento "UserDeactivated" publicado por access-service.</summary>
public sealed record UserDeactivatedV1(Guid UserId);

/// <summary>Payload v1 de <see cref="EventEnvelopeV1{TPayload}"/> para el evento "RoleAssigned" publicado por access-service.</summary>
/// <param name="PreviousRoleCode"><see langword="null"/> en la primera asignación de rol de un usuario.</param>
public sealed record RoleAssignedV1(Guid UserId, string? PreviousRoleCode, string NewRoleCode, Guid AssignedByUserId);
