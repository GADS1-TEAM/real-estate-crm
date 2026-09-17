namespace AccessService.Domain;

/// <summary>
/// Los tres roles de negocio obligatorios de la instalación única (AUTHZ-001, plan Wave 2 D3).
/// Independientes de los roles del realm de Keycloak: mismos nombres por legibilidad, pero
/// access-service es la única fuente de verdad para autorizar (D3: "los roles del realm se
/// ignoran para autorizar").
/// </summary>
public static class RoleCodes
{
    public const string Administrador = "Administrador";
    public const string Vendedor = "Vendedor";
    public const string ResponsableComercial = "Responsable Comercial";

    public static readonly IReadOnlyList<string> All = new[] { Administrador, Vendedor, ResponsableComercial };

    public static bool IsValid(string roleCode) => All.Contains(roleCode, StringComparer.Ordinal);
}
