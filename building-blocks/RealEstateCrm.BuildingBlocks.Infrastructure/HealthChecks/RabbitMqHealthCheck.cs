using Microsoft.Extensions.Diagnostics.HealthChecks;
using RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.RabbitMq;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.HealthChecks;

/// <summary>
/// Readiness check de RabbitMQ: abre y cierra un canal AMQP con la conexión compartida del
/// proceso (<see cref="RabbitMqConnectionProvider"/>, registrada por <c>AddRabbitMqMessaging</c>).
/// </summary>
public sealed class RabbitMqHealthCheck(RabbitMqConnectionProvider connectionProvider) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var channel = await connectionProvider.CreateChannelAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("No se pudo abrir un canal AMQP contra RabbitMQ.", ex);
        }
    }
}
