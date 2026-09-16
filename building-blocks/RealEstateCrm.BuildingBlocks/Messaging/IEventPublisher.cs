namespace RealEstateCrm.BuildingBlocks.Messaging;

/// <summary>
/// Puerto para publicar un <see cref="OutboxMessage"/> ya encolado hacia el broker de mensajería.
/// </summary>
/// <remarks>
/// Lo llama el proceso que drena el Outbox, nunca directamente el código de negocio
/// (que solo conoce <see cref="IOutbox"/>).
/// </remarks>
public interface IEventPublisher
{
    Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}
