// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using System;
using Microsoft.Extensions.Logging;
using TdLib.Bindings;

namespace TdLib.Extensions
{
    /// <summary>
    /// An ILogger implementation that writes log messages to TDLib's internal logging system.
    /// This allows .NET applications to route their logs through TDLib.
    /// </summary>
    public sealed class TdLibLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly TdClient _client;
        private readonly TdLogLevel _minLevel;

        /// <summary>
        /// Creates a new TdLibLogger instance
        /// </summary>
        /// <param name="categoryName">The category name for messages produced by this logger</param>
        /// <param name="client">The TdClient to use for logging</param>
        /// <param name="minLevel">The minimum log level to output</param>
        public TdLibLogger(string categoryName, TdClient client, TdLogLevel minLevel = TdLogLevel.Info)
        {
            _categoryName = categoryName ?? throw new ArgumentNullException(nameof(categoryName));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _minLevel = minLevel;
        }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            if (logLevel == LogLevel.None)
                return false;

            var tdLevel = logLevel.ToTdLogLevel();
            return (int)tdLevel <= (int)_minLevel;
        }

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));

            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message) && exception == null)
                return;

            var fullMessage = $"[{_categoryName}] {message}";
            if (exception != null)
            {
                fullMessage += Environment.NewLine + exception.ToString();
            }

            try
            {
                var tdLevel = logLevel.ToTdLogLevel();
                _client.Execute(new TdApi.AddLogMessage
                {
                    VerbosityLevel = (int)tdLevel,
                    Text = fullMessage
                });
            }
            catch (ObjectDisposedException)
            {
                // Client has been disposed, silently ignore
            }
            catch (Exception ex)
            {
                // Log to debug output to aid diagnostics without crashing the application
                System.Diagnostics.Debug.WriteLine($"Error writing log message to TDLib: {ex.Message}");
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new NullScope();
            public void Dispose() { }
        }
    }

    /// <summary>
    /// An ILoggerProvider that creates TdLibLogger instances
    /// </summary>
    public sealed class TdLibLoggerProvider : ILoggerProvider
    {
        private readonly TdClient _client;
        private readonly TdLogLevel _minLevel;

        /// <summary>
        /// Creates a new TdLibLoggerProvider
        /// </summary>
        /// <param name="client">The TdClient to use for logging</param>
        /// <param name="minLevel">The minimum log level to output</param>
        public TdLibLoggerProvider(TdClient client, TdLogLevel minLevel = TdLogLevel.Info)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _minLevel = minLevel;
        }

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
        {
            return new TdLibLogger(categoryName, _client, _minLevel);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // No resources to dispose
        }
    }

    /// <summary>
    /// Extension methods for adding TdLibLoggerProvider to ILoggerFactory
    /// </summary>
    public static class TdLibLoggerProviderExtensions
    {
        /// <summary>
        /// Adds a TdLibLoggerProvider to the logger factory
        /// </summary>
        /// <param name="factory">The ILoggerFactory to add the provider to</param>
        /// <param name="client">The TdClient to use for logging</param>
        /// <param name="minLevel">The minimum log level to output</param>
        /// <returns>The ILoggerFactory for chaining</returns>
        public static ILoggerFactory AddTdLib(this ILoggerFactory factory, TdClient client, TdLogLevel minLevel = TdLogLevel.Info)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            factory.AddProvider(new TdLibLoggerProvider(client, minLevel));
            return factory;
        }
    }
}
