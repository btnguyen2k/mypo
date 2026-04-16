using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace MyPo.Shared.Logger;

/// <summary>
/// A simple console logger that is to be used in bootstrappers where the full logging infrastructure may not be available yet.
/// </summary>
/// <code>
/// var logger = LoggerFactory.Create(b => b.AddSimpleConsoleLogger()).CreateLogger<category>();
/// logger.LogInformation("Logs are written to console");
/// </code>
public static class SimpleConsoleLoggerExtensions
{
	/// <summary>
	/// Adds the <see cref="SimpleConsoleLogger"/> to the logging builder.
	/// </summary>
	/// <param name="builder"></param>
	/// <returns></returns>
	/// <code>
	/// var logger = LoggerFactory.Create(b => b.AddSimpleConsoleLogger()).CreateLogger<category>();
	/// logger.LogInformation("Logs are written to console");
	/// </code>
	public static ILoggingBuilder AddSimpleConsoleLogger(this ILoggingBuilder builder)
    {
        builder.AddConfiguration();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SimpleConsoleLoggerProvider>());
        LoggerProviderOptions.RegisterProviderOptions<SimpleConsoleLoggerConfiguration, SimpleConsoleLoggerProvider>(builder.Services);
        return builder;
    }
}

public sealed class SimpleConsoleLoggerProvider : ILoggerProvider
{
	private readonly IDisposable? onChangeToken;
    private SimpleConsoleLoggerConfiguration config;
    private readonly ConcurrentDictionary<string, SimpleConsoleLogger> loggers = new(StringComparer.OrdinalIgnoreCase);

    public SimpleConsoleLoggerProvider(IOptionsMonitor<SimpleConsoleLoggerConfiguration> config)
    {
        this.config = config.CurrentValue;
        onChangeToken = config.OnChange(updatedConfig => this.config = updatedConfig);
    }

    public ILogger CreateLogger(string categoryName) => loggers.GetOrAdd(categoryName, name => new SimpleConsoleLogger(name, GetCurrentConfig));

    private SimpleConsoleLoggerConfiguration GetCurrentConfig() => config;

    public void Dispose()
    {
        loggers.Clear();
        onChangeToken?.Dispose();
    }
}

public sealed class SimpleConsoleLoggerConfiguration
{
    public int EventId { get; set; }

    public Dictionary<LogLevel, LogFormat> LogLevels { get; set; } =
        new()
        {
            [LogLevel.Information] = LogFormat.Short,
            [LogLevel.Warning] = LogFormat.Short,
            [LogLevel.Error] = LogFormat.Long
        };

    public enum LogFormat
    {
        Short,
        Long
    }
}

public sealed class SimpleConsoleLogger : ILogger
{
	private readonly string name;
    private readonly Func<SimpleConsoleLoggerConfiguration> getCurrentConfig;

	public SimpleConsoleLogger(string name, Func<SimpleConsoleLoggerConfiguration> getCurrentConfig) =>
        (this.name, this.getCurrentConfig) = (name, getCurrentConfig);

	/// <inheritdoc	/>
	public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default;

    public bool IsEnabled(LogLevel logLevel) => getCurrentConfig().LogLevels.ContainsKey(logLevel);

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var config = getCurrentConfig();

        if (config.EventId == 0 || config.EventId == eventId.Id)
        {
			var logLevelStr = logLevel.ToString().ToUpper().Substring(0, 4);
            switch (config.LogLevels[logLevel])
            {
                case SimpleConsoleLoggerConfiguration.LogFormat.Short:
                    Console.WriteLine($"[{logLevelStr}] {name}: {formatter(state, exception)}");
                    break;
                case SimpleConsoleLoggerConfiguration.LogFormat.Long:
                    Console.WriteLine($"[{logLevelStr}] {name}[{eventId.Id}-{eventId.Name}]:\n{formatter(state, exception)}");
                    break;
                default:
                    // No-op
                    break;
            }
        }
    }
}
