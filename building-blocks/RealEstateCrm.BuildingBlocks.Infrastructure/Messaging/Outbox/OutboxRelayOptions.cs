namespace RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.Outbox;

/// <summary>Opciones del relay reutilizable de Outbox (ver <see cref="OutboxRelayBackgroundService"/>).</summary>
public sealed class OutboxRelayOptions
{
    /// <summary>Cada cuánto el relay vuelve a consultar mensajes pendientes.</summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Cantidad máxima de mensajes que trae por ciclo (ver <see cref="Messaging.IOutbox.GetPendingAsync"/>).</summary>
    public int BatchSize { get; set; } = 20;
}
