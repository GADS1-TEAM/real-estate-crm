using RealEstateCrm.BuildingBlocks.Messaging;

namespace RealEstateCrm.TestSupport.Messaging;

/// <summary>
/// Implementación en memoria de <see cref="IOutbox"/> para unit tests: guarda los mensajes
/// encolados para que el test los inspeccione (ej. verificar el evento publicado), sin Mongo
/// real. Reutilizable por cualquier servicio.
/// </summary>
public sealed class InMemoryOutbox : IOutbox
{
    private readonly List<OutboxMessage> _messages = new();

    public IReadOnlyList<OutboxMessage> Messages => _messages;

    public Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        _messages.Add(message);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OutboxMessage>>(_messages.Where(m => !m.IsPublished).Take(batchSize).ToList());

    public Task MarkPublishedAsync(Guid eventId, DateTimeOffset publishedAt, CancellationToken cancellationToken = default)
    {
        var index = _messages.FindIndex(m => m.EventId == eventId);

        if (index >= 0)
        {
            _messages[index] = _messages[index] with { PublishedAt = publishedAt };
        }

        return Task.CompletedTask;
    }
}
