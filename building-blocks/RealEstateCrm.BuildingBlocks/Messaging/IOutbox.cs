namespace RealEstateCrm.BuildingBlocks.Messaging;

/// <summary>
/// Puerto de Outbox: encola eventos en la misma transacción que el cambio de negocio
/// (ver <see cref="Persistence.IUnitOfWork"/>) para publicarlos de forma confiable después.
/// </summary>
public interface IOutbox
{
    /// <summary>Encola un mensaje. Debe llamarse dentro de <see cref="Persistence.IUnitOfWork.ExecuteInTransactionAsync"/> para que sea atómico con el aggregate.</summary>
    Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>Trae hasta <paramref name="batchSize"/> mensajes no publicados, en orden de creación.</summary>
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>Marca un mensaje como publicado, para que no se vuelva a entregar al broker.</summary>
    Task MarkPublishedAsync(Guid eventId, DateTimeOffset publishedAt, CancellationToken cancellationToken = default);
}
