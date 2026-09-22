using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.Mongo;
using RealEstateCrm.BuildingBlocks.Messaging;
using RealEstateCrm.BuildingBlocks.Persistence;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Persistence.Mongo;

/// <summary>
/// Registro de persistencia Mongo compartida: cliente/base de datos, <see cref="IUnitOfWork"/>
/// transaccional, <see cref="IOutbox"/> e <see cref="IInbox"/>.
/// </summary>
/// <remarks>
/// No registra ningún <see cref="MongoRepository{TAggregate, TId}"/> concreto: cada servicio
/// registra el suyo (aggregate + colección propios).
/// </remarks>
public static class MongoServiceCollectionExtensions
{
    public static IServiceCollection AddMongoPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSectionName = "Mongo")
    {
        var options = new MongoOptions();
        configuration.GetSection(configurationSectionName).Bind(options);

        services.AddSingleton(options);
        services.AddSingleton<IMongoClient>(_ => new MongoClient(string.IsNullOrEmpty(options.ConnectionString) ? "mongodb://localhost:27017" : options.ConnectionString));
        services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(string.IsNullOrEmpty(options.DatabaseName) ? "crm_dev" : options.DatabaseName));

        services.AddScoped<MongoSessionAccessor>();
        services.AddScoped<IUnitOfWork, MongoUnitOfWork>();
        services.AddScoped<IOutbox, MongoOutbox>();
        services.AddSingleton<IInbox, MongoInbox>();

        return services;
    }
}
