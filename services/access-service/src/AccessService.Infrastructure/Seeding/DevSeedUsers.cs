using AccessService.Domain;

namespace AccessService.Infrastructure.Seeding;

/// <summary>
/// Los tres usuarios de desarrollo (D9), vinculados por <c>KeycloakSubject</c> a los <c>id</c>
/// fijos de <c>infra/keycloak/realm-export/crm-dev-realm.json</c> (<c>dev.administrador</c>,
/// <c>dev.vendedor</c>, <c>dev.responsable</c>). Fijar el <c>id</c> del lado de Keycloak es lo que
/// permite vincular sin integrar la Admin API (D3).
/// </summary>
internal static class DevSeedUsers
{
    public static readonly DevSeedUser Administrador = new(
        Guid.Parse("a0000000-0000-4000-8000-000000000001"), "Dev Administrador", "dev.administrador@crm-dev.local", RoleCodes.Administrador);

    public static readonly DevSeedUser Vendedor = new(
        Guid.Parse("a0000000-0000-4000-8000-000000000002"), "Dev Vendedor", "dev.vendedor@crm-dev.local", RoleCodes.Vendedor);

    public static readonly DevSeedUser ResponsableComercial = new(
        Guid.Parse("a0000000-0000-4000-8000-000000000003"), "Dev Responsable", "dev.responsable@crm-dev.local", RoleCodes.ResponsableComercial);

    public static IReadOnlyList<DevSeedUser> All { get; } = new[] { Administrador, Vendedor, ResponsableComercial };
}

internal sealed record DevSeedUser(Guid KeycloakSubject, string DisplayName, string Email, string RoleCode);
