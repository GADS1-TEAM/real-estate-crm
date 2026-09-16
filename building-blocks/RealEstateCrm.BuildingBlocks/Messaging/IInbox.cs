namespace RealEstateCrm.BuildingBlocks.Messaging;

/// <summary>
/// Puerto de Inbox: garantiza que cada consumidor procese cada <c>eventId</c> una única vez,
/// aunque RabbitMQ entregue el mensaje más de una vez (delivery at-least-once, ARCHITECTURE.md §8).
/// </summary>
public interface IInbox
{
    /// <summary>
    /// Intenta marcar <paramref name="eventId"/> como consumido por <paramref name="consumerName"/>.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> si es la primera vez que este consumidor procesa este evento
    /// (el llamador debe ejecutar el efecto). <see langword="false"/> si ya estaba marcado
    /// (el llamador debe saltear el efecto y solo confirmar el mensaje al broker).
    /// </returns>
    Task<bool> TryMarkConsumedAsync(Guid eventId, string consumerName, CancellationToken cancellationToken = default);
}
