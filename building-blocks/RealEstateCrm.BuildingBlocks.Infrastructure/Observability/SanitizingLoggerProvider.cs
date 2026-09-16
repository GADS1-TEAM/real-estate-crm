using Microsoft.Extensions.Logging;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Observability;

/// <summary>
/// Proveedor de logging estructurado que pasa cada mensaje y cada valor de scope por
/// <see cref="SensitiveDataRedactor"/> antes de escribirlo. correlationId/actorId (agregados por
/// <see cref="CorrelationIdMiddleware"/> como scope) no son campos sensibles y se preservan.
/// </summary>
public sealed class SanitizingLoggerProvider(TextWriter writer) : ILoggerProvider, ISupportExternalScope
{
    private IExternalScopeProvider? _scopeProvider;

    public ILogger CreateLogger(string categoryName) => new SanitizingLogger(categoryName, writer, () => _scopeProvider);

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopeProvider = scopeProvider;

    public void Dispose()
    {
    }
}

internal sealed class SanitizingLogger(string categoryName, TextWriter writer, Func<IExternalScopeProvider?> scopeProviderAccessor) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => scopeProviderAccessor()?.Push(state) ?? NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = SensitiveDataRedactor.Redact(formatter(state, exception));

        var scopeParts = new List<string>();
        scopeProviderAccessor()?.ForEachScope(
            (scopeValue, parts) => parts.Add(FormatScopeValue(scopeValue)),
            scopeParts);

        var line = scopeParts.Count > 0
            ? $"[{logLevel}] {categoryName}: {message} ({string.Join(", ", scopeParts)})"
            : $"[{logLevel}] {categoryName}: {message}";

        writer.WriteLine(line);

        if (exception is not null)
        {
            writer.WriteLine(SensitiveDataRedactor.Redact(exception.ToString()));
        }
    }

    private static string FormatScopeValue(object? scopeValue)
    {
        if (scopeValue is IEnumerable<KeyValuePair<string, object?>> pairs)
        {
            return string.Join(", ", pairs.Select(pair =>
            {
                var value = SensitiveDataRedactor.IsSensitiveKey(pair.Key)
                    ? SensitiveDataRedactor.RedactedPlaceholder
                    : SensitiveDataRedactor.Redact(pair.Value?.ToString() ?? string.Empty);
                return $"{pair.Key}={value}";
            }));
        }

        return SensitiveDataRedactor.Redact(scopeValue?.ToString() ?? string.Empty);
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose()
        {
        }
    }
}
