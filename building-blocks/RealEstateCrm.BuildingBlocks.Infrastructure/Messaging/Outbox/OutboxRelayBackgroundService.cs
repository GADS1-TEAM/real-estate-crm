using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RealEstateCrm.BuildingBlocks.Messaging;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.Outbox;

/// <summary>
/// Drena el <see cref="IOutbox"/> de un servicio y publica cada mensaje pendiente vía
/// <see cref="IEventPublisher"/>. Reutilizable: no sabe nada del aggregate ni del dominio del
/// servicio que lo registra, solo habla los puertos de <c>RealEstateCrm.BuildingBlocks</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Supuestos de esta POC, deliberados:</b> asume una única instancia del servicio corriendo
/// (no hay lock distribuido ni coordinación entre réplicas; con más de una instancia, dos podrían
/// intentar publicar el mismo mensaje en la misma ventana de polling — el broker/consumidor debe
/// tolerar publish duplicado, igual que ya tolera redelivery vía <c>IInbox</c>). Marca un mensaje
/// como publicado recién DESPUÉS de que <see cref="IEventPublisher.PublishAsync"/> devuelve
/// exitosamente: si el proceso muere entre el publish y el <c>MarkPublishedAsync</c>, el mensaje
/// se vuelve a publicar en el próximo ciclo (at-least-once, nunca at-most-once: es preferible un
/// duplicado ocasional a perder el evento).
/// </para>
/// <para>
/// <see cref="Messaging.IOutbox"/> se registra con lifetime Scoped (ver
/// <c>MongoServiceCollectionExtensions</c>), así que este <see cref="BackgroundService"/> abre su
/// propio <see cref="IServiceScope"/> en cada ciclo para resolverlo, en vez de inyectarlo
/// directamente (un <c>BackgroundService</c> es singleton).
/// </para>
/// </remarks>
public sealed class OutboxRelayBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxRelayOptions> options,
    ILogger<OutboxRelayBackgroundService> logger) : BackgroundService
{
    private readonly OutboxRelayOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RelayPendingBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "El relay de Outbox falló procesando un ciclo; reintenta en el próximo.");
            }

            try
            {
                await Task.Delay(_options.PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RelayPendingBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var pending = await outbox.GetPendingAsync(_options.BatchSize, cancellationToken);

        foreach (var message in pending)
        {
            try
            {
                await publisher.PublishAsync(message, cancellationToken);
                await outbox.MarkPublishedAsync(message.EventId, DateTimeOffset.UtcNow, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(
                    ex,
                    "No se pudo publicar el mensaje de Outbox {EventId} ({EventName}); queda pendiente para el próximo ciclo.",
                    message.EventId,
                    message.Name);
            }
        }
    }
}
