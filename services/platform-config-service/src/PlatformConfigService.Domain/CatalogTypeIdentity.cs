using System.Security.Cryptography;
using System.Text;

namespace PlatformConfigService.Domain;

/// <summary>
/// <c>CatalogVersionPublished</c> (CAT-006) no tiene un <see cref="CatalogEntry.EntryId"/>
/// propio: el hecho ocurre sobre el <c>catalogType</c> completo (el contador de
/// <c>ICatalogVersionPort</c>), no sobre una entrada. <see cref="EventEnvelopeV1{TPayload}.AggregateId"/>
/// no es nullable, así que este helper deriva un <see cref="Guid"/> determinístico y estable a
/// partir del <c>catalogType</c> (mismo string siempre produce el mismo id), para que todos los
/// eventos <c>CatalogVersionPublished</c> del mismo catalogType compartan un <c>aggregateId</c>
/// consistente.
/// </summary>
public static class CatalogTypeIdentity
{
    public static Guid ToAggregateId(string catalogType)
    {
        // SHA-256 truncado a 16 bytes: no es un uso de seguridad (no hay secreto ni verificación
        // de integridad acá, solo determinismo), pero se evita MD5/SHA-1 a propósito para no
        // disparar falsos positivos de los scanners de la skill aspnetcore-security-owasp-baseline.
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(catalogType));
        return new Guid(hash[..16]);
    }
}
