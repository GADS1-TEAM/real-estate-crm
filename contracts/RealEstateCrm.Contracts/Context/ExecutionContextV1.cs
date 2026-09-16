namespace RealEstateCrm.Contracts.Context;

/// <summary>
/// Contexto del actor autenticado que ejecuta una operación, propagado entre BFF, servicios y eventos.
/// </summary>
/// <remarks>
/// No incluye <c>tenantId</c> ni <c>organizationId</c>: la entrega V2 es para una única instalación
/// (ADR-001, sección 1). No agregar esos campos sin reabrir el ADR.
/// </remarks>
/// <param name="ActorId">Identificador estable del usuario autenticado (<c>sub</c> del token OIDC).</param>
/// <param name="DisplayName">Nombre para mostrar del actor.</param>
/// <param name="Email">Email del actor.</param>
/// <param name="Roles">Roles del actor tal como los resuelve el identity provider.</param>
/// <param name="Permissions">Permisos efectivos ya resueltos por la capa de autorización de negocio.</param>
/// <param name="CorrelationId">Identificador de la operación de negocio de punta a punta.</param>
/// <param name="CausationId">Identificador de la operación/evento que causó esta operación, si aplica.</param>
public sealed record ExecutionContextV1(
    Guid ActorId,
    string DisplayName,
    string Email,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions,
    Guid CorrelationId,
    Guid? CausationId);
