using System.Runtime.CompilerServices;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using PartyService.Domain;

namespace PartyService.Infrastructure.Persistence.Mongo;

/// <summary>
/// <see cref="Party"/> y <see cref="PartyRelationship"/> viven en <c>PartyService.Domain</c> (sin
/// referencia a MongoDB.Bson), así que sus class maps se registran por código acá: el id de cada
/// aggregate como <c>_id</c> y los enums (<c>identityStatus</c>, <c>commercialStatus</c>, tipos)
/// como texto legible, cada dimensión de estado en su propio campo.
/// </summary>
internal static class PartyServiceMongoClassMapBootstrap
{
    // Deliberado: mismo motivo que AccessServiceMongoClassMapBootstrap (V2-ACL-001).
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    public static void Initialize()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(PartyProfile)))
        {
            BsonClassMap.RegisterClassMap<PartyProfile>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(Party)))
        {
            BsonClassMap.RegisterClassMap<Party>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(m => m.PartyId);
                cm.MapMember(m => m.Kind).SetSerializer(new EnumSerializer<PartyKind>(BsonType.String));
                cm.MapMember(m => m.IdentityStatus).SetSerializer(new EnumSerializer<IdentityStatus>(BsonType.String));
                cm.MapMember(m => m.CommercialStatus).SetSerializer(new EnumSerializer<CommercialStatus>(BsonType.String));
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(PartyRelationship)))
        {
            BsonClassMap.RegisterClassMap<PartyRelationship>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(m => m.RelationshipId);
                cm.MapMember(m => m.RelationshipType).SetSerializer(new EnumSerializer<RelationshipType>(BsonType.String));
                cm.SetIgnoreExtraElements(true);
            });
        }
    }
}
