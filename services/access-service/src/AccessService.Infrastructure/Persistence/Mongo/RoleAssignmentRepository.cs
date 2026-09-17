using AccessService.Domain;
using MongoDB.Driver;
using RealEstateCrm.BuildingBlocks.Infrastructure.Persistence.Mongo;

namespace AccessService.Infrastructure.Persistence.Mongo;

/// <summary>
/// Repositorio de <see cref="RoleAssignment"/> sobre la colección <c>role_assignments</c> (D8).
/// El <c>_id</c> es <see cref="RoleAssignment.UserId"/> (ver <see cref="AccessServiceMongoClassMapBootstrap"/>):
/// <c>GetByIdAsync(userId)</c> resuelve directamente "el rol activo de este usuario", sin filtro
/// adicional, porque solo puede existir un documento por usuario.
/// </summary>
public sealed class RoleAssignmentRepository(IMongoDatabase database, MongoSessionAccessor sessionAccessor)
    : VersionedMongoRepository<RoleAssignment, Guid>(
        database,
        collectionName: "role_assignments",
        sessionAccessor,
        idSelector: assignment => assignment.UserId,
        versionSelector: assignment => assignment.Version);
