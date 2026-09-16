using RealEstateCrm.Contracts.Events;

namespace RealEstateCrm.BuildingBlocks.Messaging;

/// <summary>
/// Puerto que implementa un servicio para procesar un evento de integración de un tipo dado.
/// </summary>
/// <remarks>
/// No decide topología (exchange/routing key/cola/DLQ): eso lo define el adapter de
/// RabbitMQ en <c>RealEstateCrm.BuildingBlocks.Infrastructure</c> al registrar el consumer.
/// El host de consumo ya resuelve <see cref="IInbox"/> antes de llamar a
/// <see cref="HandleAsync"/>, así que una implementación no necesita preocuparse por
/// duplicados.
/// </remarks>
/// <typeparam name="TPayload">Forma del payload del evento que este consumer entiende.</typeparam>
public interface IEventConsumer<TPayload>
{
    /// <summary>Nombre estable del consumer, usado como clave de idempotencia en el Inbox.</summary>
    string ConsumerName { get; }

    Task HandleAsync(EventEnvelopeV1<TPayload> envelope, CancellationToken cancellationToken = default);
}
