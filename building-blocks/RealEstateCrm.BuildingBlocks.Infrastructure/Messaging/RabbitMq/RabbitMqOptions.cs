namespace RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.RabbitMq;

/// <summary>
/// Opciones de conexión a RabbitMQ y nombre del exchange topic propio del servicio.
/// </summary>
/// <remarks>
/// Topología decidida por el equipo (V2-FND-002): un exchange topic por servicio productor,
/// routing key = nombre del evento, una cola + DLQ por consumidor
/// (ver <see cref="RabbitMqConsumerRegistration"/>).
/// </remarks>
public sealed class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    public string VirtualHost { get; set; } = "/";

    /// <summary>Nombre del exchange topic donde este servicio publica sus eventos, ej. "party-service.events".</summary>
    public string ServiceExchangeName { get; set; } = string.Empty;
}
