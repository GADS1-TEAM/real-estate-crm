using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RealEstateCrm.BuildingBlocks.Infrastructure.Observability;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Tests.Observability;

/// <summary>
/// OPS-005 de punta a punta contra <see cref="ILogger"/> real (no solo la función pura de
/// <see cref="SensitiveDataRedactor"/>): un mensaje y un scope con campos sensibles se
/// redactan antes de escribirse, mientras que correlationId/actorId del scope se preservan.
/// </summary>
public class SanitizingLoggerProviderTests
{
    private static (ILogger Logger, StringWriter Output, IDisposable Host) CreateLogger()
    {
        var output = new StringWriter();
        var services = new ServiceCollection();
        services.AddLogging(logging =>
        {
            logging.AddProvider(new SanitizingLoggerProvider(output));
            logging.SetMinimumLevel(LogLevel.Trace);
        });
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("TestCategory");
        return (logger, output, provider);
    }

    [Fact]
    public void Message_with_sensitive_data_is_redacted_in_the_written_log_line()
    {
        var (logger, output, host) = CreateLogger();
        using (host)
        {
            logger.LogInformation("Login attempt password=hunter2 for user=ana");
        }

        var written = output.ToString();
        Assert.DoesNotContain("hunter2", written);
        Assert.Contains("user=ana", written);
    }

    [Fact]
    public void CorrelationId_and_actorId_from_scope_survive_while_sensitive_scope_values_are_redacted()
    {
        var (logger, output, host) = CreateLogger();
        using (host)
        {
            var scope = new Dictionary<string, object?>
            {
                ["correlationId"] = "cid-123",
                ["actorId"] = "actor-456",
                ["token"] = "super-secret-token",
            };

            using (logger.BeginScope(scope))
            {
                logger.LogInformation("Processing request");
            }
        }

        var written = output.ToString();
        Assert.Contains("cid-123", written);
        Assert.Contains("actor-456", written);
        Assert.DoesNotContain("super-secret-token", written);
    }

    [Fact]
    public void Exception_message_and_stack_trace_are_redacted_when_they_contain_sensitive_data()
    {
        var (logger, output, host) = CreateLogger();
        using (host)
        {
            var exception = new InvalidOperationException("Falló con password=hunter2");
            logger.LogError(exception, "Unhandled error");
        }

        var written = output.ToString();
        Assert.DoesNotContain("hunter2", written);
    }
}
