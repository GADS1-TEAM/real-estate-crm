using System.Runtime.CompilerServices;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Persistence.Mongo;

/// <summary>
/// Desde MongoDB.Driver 3.x, <see cref="Guid"/> ya no tiene una representación BSON por
/// defecto (<see cref="BsonSerializationException"/> "GuidRepresentation is Unspecified") y
/// hay que declararla explícitamente. Se fija <see cref="GuidRepresentation.Standard"/> (el
/// subtipo binario 4, compatible entre drivers) una única vez por proceso, para que todos los
/// aggregates/documentos de todos los servicios serialicen Guid de la misma forma sin que cada
/// uno tenga que configurarlo.
/// </summary>
internal static class MongoGuidSerializationBootstrap
{
    // Deliberado: los adapters de este building block (MongoRepository<T,TId>, MongoOutbox,
    // MongoInbox) se pueden construir directamente, sin pasar por AddMongoPersistence. Un
    // ModuleInitializer es la única forma de garantizar el registro sin importar cómo se
    // instancien.
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    public static void Initialize()
    {
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        BsonSerializer.RegisterSerializer(new NullableSerializer<Guid>(new GuidSerializer(GuidRepresentation.Standard)));
    }
}
