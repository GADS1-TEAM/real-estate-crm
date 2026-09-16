namespace RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.RabbitMq;

/// <summary>
/// Topología de una cola de consumo: de qué exchange/routing key escucha, y a qué cola
/// (con su propia DLQ) entrega. La decide quien registra el consumer, no el puerto
/// <see cref="Messaging.IEventConsumer{TPayload}"/>.
/// </summary>
/// <param name="ExchangeName">Exchange topic del servicio productor.</param>
/// <param name="RoutingKey">Routing key a bindear (nombre del evento, o patrón con "*"/"#").</param>
/// <param name="QueueName">Cola propia de este consumidor.</param>
/// <param name="DeadLetterQueueName">Cola donde caen los mensajes que el consumer no pudo procesar.</param>
public sealed record RabbitMqConsumerRegistration(
    string ExchangeName,
    string RoutingKey,
    string QueueName,
    string? DeadLetterQueueName = null)
{
    public string ResolvedDeadLetterQueueName => DeadLetterQueueName ?? $"{QueueName}.dlq";
}
