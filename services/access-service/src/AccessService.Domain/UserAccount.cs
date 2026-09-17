namespace AccessService.Domain;

/// <summary>
/// Aggregate root de una cuenta de usuario (USR-001/USR-002). No es una Party (regla de
/// V2-ACL-001): identidad de acceso al sistema, no un contacto/empresa de negocio.
/// </summary>
/// <remarks>
/// Inmutable (record): cada método de mutación devuelve una nueva instancia con
/// <see cref="Version"/> incrementado, que la Application layer persiste vía
/// <c>IRepository{UserAccount, Guid}.UpdateAsync</c>. El repositorio concreto
/// (<c>UserAccountRepository</c> en <c>AccessService.Infrastructure</c>, sobre
/// <c>VersionedMongoRepository</c> de <c>BuildingBlocks.Infrastructure</c>) usa este
/// <see cref="Version"/> para concurrencia optimista. El Domain no referencia ningún tipo de
/// <c>BuildingBlocks</c> a propósito (pureza de dominio, ver test de arquitectura
/// <c>Domain_projects_do_not_reference_infrastructure_frameworks_or_other_projects</c>).
/// </remarks>
/// <param name="UserId">Identificador propio de access-service (no confundir con <see cref="KeycloakSubject"/>).</param>
/// <param name="KeycloakSubject">Claim <c>sub</c> del token OIDC que identifica al usuario en Keycloak (D3).</param>
public sealed record UserAccount(
    Guid UserId,
    Guid KeycloakSubject,
    string DisplayName,
    string Email,
    UserStatus Status,
    int Version)
{
    /// <summary>Alta directa por un Administrador (USR-001): el usuario queda habilitado de inmediato.</summary>
    public static UserAccount CreateActive(Guid keycloakSubject, string displayName, string email) =>
        new(Guid.NewGuid(), keycloakSubject, displayName, email, UserStatus.Active, Version: 1);

    /// <summary>
    /// Auto-provisioning del primer login de un <c>sub</c> desconocido (D3): sin permisos hasta
    /// que un Administrador lo habilite asignándole un rol (ver <see cref="Activate"/>).
    /// </summary>
    public static UserAccount CreatePending(Guid keycloakSubject, string displayName, string email) =>
        new(Guid.NewGuid(), keycloakSubject, displayName, email, UserStatus.Pending, Version: 1);

    /// <summary>Edición de perfil (USR-001). No cambia <see cref="Status"/>.</summary>
    public UserAccount UpdateProfile(string displayName, string email) =>
        this with { DisplayName = displayName, Email = email, Version = Version + 1 };

    /// <summary>Habilita un usuario PENDING o INACTIVE. Sin efecto si ya está ACTIVE.</summary>
    public UserAccount Activate() =>
        Status == UserStatus.Active ? this : this with { Status = UserStatus.Active, Version = Version + 1 };

    /// <summary>
    /// Desactiva el usuario (USR-002). No elimina ni muta ninguna otra colección: la historia de
    /// sus actividades/registros asociados queda intacta, solo cambia <see cref="Status"/>.
    /// </summary>
    public UserAccount Deactivate()
    {
        if (Status == UserStatus.Inactive)
        {
            throw new InvalidOperationException("El usuario ya está INACTIVE.");
        }

        return this with { Status = UserStatus.Inactive, Version = Version + 1 };
    }
}
