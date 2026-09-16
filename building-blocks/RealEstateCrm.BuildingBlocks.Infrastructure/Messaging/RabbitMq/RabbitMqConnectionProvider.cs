using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.RabbitMq;

/// <summary>
/// Conexión AMQP compartida del proceso. RabbitMQ.Client recomienda una sola <see cref="IConnection"/>
/// por proceso y abrir un <see cref="IChannel"/> por unidad de trabajo (publisher/consumer).
/// </summary>
/// <remarks>
/// Registrado como singleton; la creación es perezosa y thread-safe
/// (<see cref="Lazy{T}"/> con <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/>,
/// ver skill dotnet-thread-safety-and-shared-state) para no abrir la conexión hasta el primer uso.
/// </remarks>
public sealed class RabbitMqConnectionProvider : IAsyncDisposable
{
    private readonly Lazy<Task<IConnection>> _connection;

    public RabbitMqConnectionProvider(IOptions<RabbitMqOptions> options)
    {
        var value = options.Value;

        _connection = new Lazy<Task<IConnection>>(
            () => CreateConnectionAsync(value),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public async Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default)
    {
        var connection = await _connection.Value;
        return await connection.CreateChannelAsync(cancellationToken: cancellationToken);
    }

    private static async Task<IConnection> CreateConnectionAsync(RabbitMqOptions options)
    {
        var factory = new ConnectionFactory
        {
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password,
            VirtualHost = options.VirtualHost,
        };

        return await factory.CreateConnectionAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection.IsValueCreated)
        {
            var connection = await _connection.Value;
            await connection.DisposeAsync();
        }
    }
}
