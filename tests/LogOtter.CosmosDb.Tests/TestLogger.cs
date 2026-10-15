using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace LogOtter.CosmosDb.Tests;

public record LogEntry(LogLevel Level, string Message);

public class TestLogger<T> : ILogger<T>
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();

    public IReadOnlyCollection<LogEntry> Entries => _entries;

    public event Action<LogEntry>? Logged;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var entry = new LogEntry(logLevel, formatter(state, exception));
        _entries.Enqueue(entry);
        Logged?.Invoke(entry);
    }
}
