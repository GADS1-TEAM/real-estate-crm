using System.Runtime.CompilerServices;
using MongoDB.Bson.Serialization;
using PlatformConfigService.Domain;

namespace PlatformConfigService.Infrastructure.Persistence.Mongo;

/// <summary>
/// <see cref="CatalogEntry"/> vive en <c>PlatformConfigService.Domain</c> (sin referencia a
/// MongoDB.Bson: pureza de dominio), así que no puede llevar atributos <c>[BsonId]</c>. Este
/// class map, registrado por código acá en Infrastructure, mapea <c>EntryId</c> como <c>_id</c>
/// (mismo patrón que <c>AccessServiceMongoClassMapBootstrap</c>, V2-ACL-001).
/// </summary>
internal static class PlatformConfigServiceMongoClassMapBootstrap
{
    // Deliberado: mismo motivo que MongoGuidSerializationBootstrap/AccessServiceMongoClassMapBootstrap.
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    public static void Initialize()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(CatalogEntry)))
        {
            BsonClassMap.RegisterClassMap<CatalogEntry>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(m => m.EntryId);
                cm.SetIgnoreExtraElements(true);
            });
        }
    }
}
