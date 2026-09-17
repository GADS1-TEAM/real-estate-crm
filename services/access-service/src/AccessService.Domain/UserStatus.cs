namespace AccessService.Domain;

/// <summary>Estado de habilitación de un <see cref="UserAccount"/> (USR-001/USR-002, D3).</summary>
public enum UserStatus
{
    /// <summary>Primer login de un <c>sub</c> de Keycloak desconocido: sin permisos hasta que un Administrador lo habilite (D3).</summary>
    Pending,

    /// <summary>Usuario habilitado: puede iniciar sesión y ejercer los permisos de su rol.</summary>
    Active,

    /// <summary>Usuario desactivado por un Administrador. No borra su historia (USR-002).</summary>
    Inactive,
}
