using AccessService.Application.Authorization;
using AccessService.Application.Ports;
using AccessService.Domain;
using RealEstateCrm.BuildingBlocks.Messaging;
using RealEstateCrm.BuildingBlocks.Persistence;
using RealEstateCrm.Contracts.Context;
using RealEstateCrm.Contracts.Events;
using RealEstateCrm.Contracts.Events.Access;
using RealEstateCrm.Contracts.Paging;

namespace AccessService.Application.Users;

/// <summary>
/// Application service de access-service: implementa los commands/queries de V2-ACL-001
/// (CreateUser, UpdateUser, DeactivateUser, AssignRole, GetUsers, GetEffectivePermissions).
/// </summary>
/// <remarks>
/// Sin capa de mediator/CQRS: la skill correspondiente no está habilitada para Wave 2 todavía
/// (plan Wave 2, sección 4). Cada método público es, en efecto, un command/query handler.
/// </remarks>
public sealed class UserAccountService(
    IRepository<UserAccount, Guid> userAccounts,
    IRepository<RoleAssignment, Guid> roleAssignments,
    IUserAccountReadPort userAccountReads,
    IUnitOfWork unitOfWork,
    IOutbox outbox,
    TimeProvider timeProvider)
{
    /// <summary>USR-001: alta de usuario por un Administrador (ya autorizado por el caller vía <c>IAuthorizationPort</c>).</summary>
    public async Task<UserSummary> CreateUserAsync(
        ExecutionContextV1 context,
        Guid keycloakSubject,
        string displayName,
        string email,
        CancellationToken cancellationToken = default)
    {
        var existing = await userAccountReads.GetByKeycloakSubjectAsync(keycloakSubject, cancellationToken);

        if (existing is not null)
        {
            throw new AccessDomainException(
                AccessErrorCodes.UserAlreadyExists,
                httpStatus: 409,
                $"Ya existe un usuario para el sub de Keycloak '{keycloakSubject}'.");
        }

        var user = UserAccount.CreateActive(keycloakSubject, displayName, email);

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await userAccounts.AddAsync(user, token);
            await EnqueueEventAsync(
                context,
                "UserCreated",
                version: 1,
                aggregateId: user.UserId,
                new UserCreatedV1(user.UserId, user.DisplayName, user.Email, user.Status.ToString()),
                token);
        }, cancellationToken);

        return ToSummary(user, roleCode: null);
    }

    /// <summary>USR-001: edición de perfil.</summary>
    public async Task<UserSummary> UpdateUserAsync(
        ExecutionContextV1 context,
        Guid userId,
        string displayName,
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId, cancellationToken);
        var updated = user.UpdateProfile(displayName, email);

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await userAccounts.UpdateAsync(updated, token);
            await EnqueueEventAsync(
                context,
                "UserUpdated",
                version: 1,
                aggregateId: updated.UserId,
                new UserUpdatedV1(updated.UserId, updated.DisplayName, updated.Email),
                token);
        }, cancellationToken);

        var roleAssignment = await roleAssignments.GetByIdAsync(updated.UserId, cancellationToken);
        return ToSummary(updated, roleAssignment?.RoleCode);
    }

    /// <summary>USR-002: desactivación. No borra ni muta ninguna otra colección: solo cambia el status.</summary>
    public async Task<UserSummary> DeactivateUserAsync(
        ExecutionContextV1 context,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId, cancellationToken);

        UserAccount deactivated;
        try
        {
            deactivated = user.Deactivate();
        }
        catch (InvalidOperationException ex)
        {
            throw new AccessDomainException(AccessErrorCodes.UserAlreadyInactive, httpStatus: 409, ex.Message);
        }

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await userAccounts.UpdateAsync(deactivated, token);
            await EnqueueEventAsync(
                context,
                "UserDeactivated",
                version: 1,
                aggregateId: deactivated.UserId,
                new UserDeactivatedV1(deactivated.UserId),
                token);
        }, cancellationToken);

        var roleAssignment = await roleAssignments.GetByIdAsync(deactivated.UserId, cancellationToken);
        return ToSummary(deactivated, roleAssignment?.RoleCode);
    }

    /// <summary>
    /// AUTHZ-001: asigna UNO de los tres roles obligatorios. Reemplaza el rol activo si ya
    /// tenía uno (invariante de <see cref="RoleAssignment"/>: un solo rol por usuario). Si el
    /// usuario destino estaba PENDING, queda habilitado (D3: "el Administrador lo habilita y le
    /// asigna rol").
    /// </summary>
    public async Task<UserSummary> AssignRoleAsync(
        ExecutionContextV1 context,
        Guid userId,
        string roleCode,
        CancellationToken cancellationToken = default)
    {
        if (!RoleCodes.IsValid(roleCode))
        {
            throw new AccessDomainException(
                AccessErrorCodes.InvalidRoleCode,
                httpStatus: 422,
                $"'{roleCode}' no es uno de los tres roles obligatorios ({string.Join(", ", RoleCodes.All)}).");
        }

        var targetUser = await RequireUserAsync(userId, cancellationToken);
        var actingUser = await userAccountReads.GetByKeycloakSubjectAsync(context.ActorId, cancellationToken)
            ?? throw new AccessDomainException(AccessErrorCodes.UserNotFound, httpStatus: 404, "No se pudo resolver el usuario actuante.");

        var existingAssignment = await roleAssignments.GetByIdAsync(userId, cancellationToken);
        var previousRoleCode = existingAssignment?.RoleCode;
        var now = timeProvider.GetUtcNow();

        var newAssignment = existingAssignment is null
            ? RoleAssignment.Create(userId, roleCode, actingUser.UserId, now)
            : existingAssignment.Reassign(roleCode, actingUser.UserId, now);

        var activatedUser = targetUser.Status == UserStatus.Pending ? targetUser.Activate() : null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            if (existingAssignment is null)
            {
                await roleAssignments.AddAsync(newAssignment, token);
            }
            else
            {
                await roleAssignments.UpdateAsync(newAssignment, token);
            }

            if (activatedUser is not null)
            {
                await userAccounts.UpdateAsync(activatedUser, token);
            }

            await EnqueueEventAsync(
                context,
                "RoleAssigned",
                version: 1,
                aggregateId: userId,
                new RoleAssignedV1(userId, previousRoleCode, roleCode, actingUser.UserId),
                token);
        }, cancellationToken);

        return ToSummary(activatedUser ?? targetUser, roleCode);
    }

    /// <summary>GetUsers (paginado).</summary>
    public async Task<PageV1<UserSummary>> GetUsersAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var users = await userAccountReads.SearchAsync(page, pageSize, cancellationToken);

        var items = new List<UserSummary>(users.Items.Count);
        foreach (var user in users.Items)
        {
            var roleAssignment = await roleAssignments.GetByIdAsync(user.UserId, cancellationToken);
            items.Add(ToSummary(user, roleAssignment?.RoleCode));
        }

        return new PageV1<UserSummary>(items, users.Page, users.PageSize, users.TotalCount);
    }

    /// <summary>
    /// AUTHZ-002: permisos efectivos de un usuario. PENDING/INACTIVE siempre devuelven una lista
    /// vacía, sin importar el rol asignado (mismo orden de precedencia que
    /// <see cref="AccessServiceAuthorizationEvaluator"/>).
    /// </summary>
    public async Task<EffectivePermissionsResult> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId, cancellationToken);

        if (user.Status != UserStatus.Active)
        {
            return new EffectivePermissionsResult(userId, RoleCode: null, Permissions: Array.Empty<string>());
        }

        var roleAssignment = await roleAssignments.GetByIdAsync(userId, cancellationToken);

        if (roleAssignment is null)
        {
            return new EffectivePermissionsResult(userId, RoleCode: null, Permissions: Array.Empty<string>());
        }

        return new EffectivePermissionsResult(userId, roleAssignment.RoleCode, PermissionMatrix.PermissionsForRole(roleAssignment.RoleCode));
    }

    private async Task<UserAccount> RequireUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await userAccounts.GetByIdAsync(userId, cancellationToken)
        ?? throw new AccessDomainException(AccessErrorCodes.UserNotFound, httpStatus: 404, $"No existe el usuario '{userId}'.");

    private Task EnqueueEventAsync<TPayload>(
        ExecutionContextV1 context,
        string name,
        int version,
        Guid aggregateId,
        TPayload payload,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        var envelope = new EventEnvelopeV1<TPayload>(
            EventId: Guid.NewGuid(),
            Name: name,
            Version: version,
            OccurredAt: now,
            ActorId: context.ActorId,
            CorrelationId: context.CorrelationId,
            CausationId: context.CausationId,
            AggregateId: aggregateId,
            Payload: payload);

        return outbox.EnqueueAsync(OutboxMessage.From(envelope, now), cancellationToken);
    }

    private static UserSummary ToSummary(UserAccount user, string? roleCode) =>
        new(
            user.UserId,
            user.DisplayName,
            user.Email,
            user.Status.ToString(),
            roleCode is null ? Array.Empty<string>() : new[] { roleCode });
}
