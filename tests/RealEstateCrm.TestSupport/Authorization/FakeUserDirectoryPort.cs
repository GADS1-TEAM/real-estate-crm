using RealEstateCrm.BuildingBlocks.Authorization;
using RealEstateCrm.Contracts.Users;

namespace RealEstateCrm.TestSupport.Authorization;

/// <summary>
/// Implementación en memoria de <see cref="IUserDirectoryPort"/> para tests de servicios owner:
/// mapea el <c>sub</c> del actor a su <see cref="UserSelfV1"/>. Un <c>sub</c> no registrado se
/// deniega como <c>user_pending</c> (mismo trato que un login desconocido, D3).
/// </summary>
public sealed class FakeUserDirectoryPort : IUserDirectoryPort
{
    private readonly Dictionary<Guid, UserSelfResult> _users = new();

    public FakeUserDirectoryPort WithActiveUser(Guid actorSubject, Guid userId, string? roleCode)
    {
        _users[actorSubject] = UserSelfResult.Active(new UserSelfV1(userId, "Active", roleCode));
        return this;
    }

    public FakeUserDirectoryPort WithDeniedUser(Guid actorSubject, string reasonCode)
    {
        _users[actorSubject] = UserSelfResult.Denied(reasonCode);
        return this;
    }

    public Task<UserSelfResult> GetSelfAsync(Guid actorId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.TryGetValue(actorId, out var result) ? result : UserSelfResult.Denied("user_pending"));
}
