namespace RealEstateCrm.BuildingBlocks.Infrastructure.Persistence.Mongo;

/// <summary>
/// Opciones de conexión a la instancia MongoDB compartida (ARCHITECTURE.md §7: una instancia,
/// ownership lógico por servicio). Se bindean desde la sección "Mongo" de cada Api.
/// </summary>
public sealed class MongoOptions
{
    /// <summary>Connection string de la instancia Mongo compartida.</summary>
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";

    /// <summary>Base de datos lógica de este servicio (ej. "party_service").</summary>
    public string DatabaseName { get; set; } = string.Empty;
}
