namespace RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.Mongo;

/// <summary>
/// Marca de que <see cref="ConsumerName"/> ya procesó <see cref="EventId"/>. La unicidad de
/// (EventId, ConsumerName) es lo que garantiza la idempotencia (ver <see cref="MongoInbox"/>).
/// </summary>
internal sealed record InboxConsumedMessage(Guid EventId, string ConsumerName, DateTimeOffset ConsumedAt);
