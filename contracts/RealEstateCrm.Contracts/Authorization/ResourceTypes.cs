namespace RealEstateCrm.Contracts.Authorization;

/// <summary>
/// Valores estables de <c>resourceType</c> para
/// <see cref="RealEstateCrm.BuildingBlocks.Authorization.IAuthorizationPort"/>.
/// </summary>
/// <remarks>
/// El parámetro sigue siendo <see cref="string"/> en el puerto (lo evalúa un servicio HTTP
/// externo, access-service): estas constantes solo evitan que cada caller invente su propio
/// literal. Waves siguientes agregan acá el tipo de recurso que introduzcan.
/// </remarks>
public static class ResourceTypes
{
    public const string User = "user";
    public const string Catalog = "catalog";
    public const string Party = "party";
}
