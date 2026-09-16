using System.Runtime.CompilerServices;
using MongoDB.Bson.Serialization;
using RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.Mongo;
using RealEstateCrm.BuildingBlocks.Messaging;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Persistence.Mongo;

/// <summary>
/// <see cref="OutboxMessage"/> vive en <c>RealEstateCrm.BuildingBlocks</c> (puertos, sin
/// referencia a MongoDB.Bson), así que no puede llevar atributos <c>[BsonId]</c>. Estos class
/// maps, registrados por código acá en <c>Infrastructure</c>, mapean <c>EventId</c> como
/// <c>_id</c> de Mongo (evita un ObjectId autogenerado redundante y usa la clave natural del
/// evento) para los dos tipos que persiste el outbox/inbox del building block.
/// </summary>
internal static class MongoClassMapBootstrap
{
    // Deliberado: mismo motivo que MongoGuidSerializationBootstrap (ver ahí).
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    public static void Initialize()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(OutboxMessage)))
        {
            BsonClassMap.RegisterClassMap<OutboxMessage>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(m => m.EventId);
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(InboxConsumedMessage)))
        {
            BsonClassMap.RegisterClassMap<InboxConsumedMessage>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }
    }
}
