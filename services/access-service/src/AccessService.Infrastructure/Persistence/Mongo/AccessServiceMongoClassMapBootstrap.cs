using System.Runtime.CompilerServices;
using AccessService.Domain;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace AccessService.Infrastructure.Persistence.Mongo;

/// <summary>
/// <see cref="UserAccount"/> y <see cref="RoleAssignment"/> viven en
/// <c>AccessService.Domain</c> (sin referencia a MongoDB.Bson: pureza de dominio), así que no
/// pueden llevar atributos <c>[BsonId]</c>/<c>[BsonRepresentation]</c>. Estos class maps,
/// registrados por código acá en Infrastructure, mapean <c>UserId</c> como <c>_id</c> de ambas
/// colecciones (mismo id en las dos: <see cref="RoleAssignment.UserId"/> es también su
/// <c>_id</c>, así se enforcea "un solo rol activo por usuario" por construcción) y serializan
/// <see cref="UserStatus"/> como string legible en vez del entero por defecto.
/// </summary>
internal static class AccessServiceMongoClassMapBootstrap
{
    // Deliberado: mismo motivo que MongoGuidSerializationBootstrap/MongoClassMapBootstrap en
    // RealEstateCrm.BuildingBlocks.Infrastructure (V2-FND-002).
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    public static void Initialize()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(UserAccount)))
        {
            BsonClassMap.RegisterClassMap<UserAccount>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(m => m.UserId);
                cm.MapMember(m => m.Status).SetSerializer(new EnumSerializer<UserStatus>(BsonType.String));
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(RoleAssignment)))
        {
            BsonClassMap.RegisterClassMap<RoleAssignment>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(m => m.UserId);
                cm.SetIgnoreExtraElements(true);
            });
        }
    }
}
