// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using TdLib.Bindings;

namespace TdLib.Extensions
{
    /// <summary>
    /// Delegate for TDLib log message callback
    /// </summary>
    /// <param name="verbosityLevel">The verbosity level of the message</param>
    /// <param name="message">The log message</param>
    public delegate void LogMessageCallback(int verbosityLevel, string message);

    /// <summary>
    /// Provides extension methods for integrating TDLib logging with Microsoft.Extensions.Logging.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Thread Safety:</b> The UseTdLibLogging methods are not thread-safe.
    /// They should be called once during application initialization before using the TdClient.
    /// </para>
    /// <para>
    /// <b>Multi-Client Scenarios:</b> TDLib uses a global fatal error callback, so only one
    /// logger can be configured for fatal errors at a time. If you need different loggers
    /// for different TdClient instances, use the <see cref="TdLibLoggerProvider"/> instead
    /// for application-to-TDLib logging.
    /// </para>
    /// </remarks>
    public static class TdLoggerExtensions
    {
        private static readonly object _lock = new object();
        private static ILogger _logger;
        private static Callback _nativeCallback;

        /// <summary>
        /// Configures TDLib to use the specified ILogger for logging fatal errors.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is not thread-safe and should be called once during application initialization.
        /// </para>
        /// <para>
        /// TDLib native bindings only support a global fatal error callback.
        /// For full logging integration where TDLib logs are routed to ILogger, consider
        /// using a file-based log stream and monitoring the log file.
        /// </para>
        /// </remarks>
        /// <param name="client">The TdClient instance</param>
        /// <param name="logger">The ILogger to use for logging</param>
        /// <param name="logLevel">The TDLib log level to set</param>
        public static void UseTdLibLogging(this TdClient client, ILogger logger, TdLogLevel logLevel = TdLogLevel.Warning)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            lock (_lock)
            {
                _logger = logger;

                // Set the verbosity level
                client.Bindings.SetLogVerbosityLevel(logLevel);

                // Set up the fatal error callback to route to ILogger
                // Keep a reference to prevent garbage collection
                _nativeCallback = OnFatalError;
                client.Bindings.SetLogFatalErrorCallback(_nativeCallback);
            }
        }

        /// <summary>
        /// Configures TDLib to use the specified ILogger for logging, 
        /// with an option to disable default logging output.
        /// </summary>
        /// <param name="client">The TdClient instance</param>
        /// <param name="logger">The ILogger to use for logging</param>
        /// <param name="logLevel">The TDLib log level to set</param>
        /// <param name="disableDefaultLogging">Whether to disable default console/stderr logging</param>
        public static void UseTdLibLogging(this TdClient client, ILogger logger, TdLogLevel logLevel, bool disableDefaultLogging)
        {
            UseTdLibLogging(client, logger, logLevel);

            if (disableDefaultLogging)
            {
                // Disable default logging by setting an empty log stream
                client.Execute(new TdApi.SetLogStream
                {
                    LogStream = new TdApi.LogStream.LogStreamEmpty()
                });
            }
        }

        private static void OnFatalError(IntPtr messagePtr)
        {
            ILogger currentLogger;
            lock (_lock)
            {
                currentLogger = _logger;
            }

            if (currentLogger == null || messagePtr == IntPtr.Zero)
                return;

            try
            {
                var message = Marshal.PtrToStringAnsi(messagePtr);
                currentLogger.LogCritical("[TDLib Fatal] {Message}", message);
            }
            catch (Exception ex)
            {
                // Swallow exceptions in callback to prevent native crashes
                // Use Debug.WriteLine for diagnostics as we cannot use the logger here
                System.Diagnostics.Debug.WriteLine($"Error in TDLib fatal error callback: {ex}");
            }
        }

        /// <summary>
        /// Maps TDLib verbosity level to Microsoft.Extensions.Logging LogLevel.
        /// </summary>
        /// <remarks>
        /// Both <see cref="TdLogLevel.Verbose"/> and <see cref="TdLogLevel.All"/> map to
        /// <see cref="LogLevel.Trace"/> because .NET's LogLevel has fewer granularity levels
        /// than TDLib's verbosity system. TDLib's All (1024) represents maximum verbosity,
        /// which semantically aligns with Trace in the .NET logging hierarchy.
        /// </remarks>
        /// <param name="tdLogLevel">TDLib verbosity level (0-5+)</param>
        /// <returns>Corresponding Microsoft.Extensions.Logging LogLevel</returns>
        public static LogLevel ToLogLevel(this TdLogLevel tdLogLevel)
        {
            switch (tdLogLevel)
            {
                case TdLogLevel.Fatal:
                    return LogLevel.Critical;
                case TdLogLevel.Error:
                    return LogLevel.Error;
                case TdLogLevel.Warning:
                    return LogLevel.Warning;
                case TdLogLevel.Info:
                    return LogLevel.Information;
                case TdLogLevel.Debug:
                    return LogLevel.Debug;
                case TdLogLevel.Verbose:
                    // Verbose maps to Trace - highest verbosity in .NET
                    return LogLevel.Trace;
                case TdLogLevel.All:
                    // All (1024) is TDLib's maximum verbosity, also maps to Trace
                    return LogLevel.Trace;
                default:
                    return LogLevel.Information;
            }
        }

        /// <summary>
        /// Maps TDLib verbosity level integer to Microsoft.Extensions.Logging LogLevel
        /// </summary>
        /// <param name="verbosityLevel">TDLib verbosity level (0-5+)</param>
        /// <returns>Corresponding Microsoft.Extensions.Logging LogLevel</returns>
        public static LogLevel ToLogLevel(int verbosityLevel)
        {
            if (verbosityLevel <= 0)
                return LogLevel.Critical;
            if (verbosityLevel == 1)
                return LogLevel.Error;
            if (verbosityLevel == 2)
                return LogLevel.Warning;
            if (verbosityLevel == 3)
                return LogLevel.Information;
            if (verbosityLevel == 4)
                return LogLevel.Debug;
            // Levels 5+ (including 1024 for All) map to Trace
            return LogLevel.Trace;
        }

        /// <summary>
        /// Maps Microsoft.Extensions.Logging LogLevel to TDLib verbosity level
        /// </summary>
        /// <param name="logLevel">Microsoft.Extensions.Logging LogLevel</param>
        /// <returns>Corresponding TDLib verbosity level</returns>
        public static TdLogLevel ToTdLogLevel(this LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Critical:
                    return TdLogLevel.Fatal;
                case LogLevel.Error:
                    return TdLogLevel.Error;
                case LogLevel.Warning:
                    return TdLogLevel.Warning;
                case LogLevel.Information:
                    return TdLogLevel.Info;
                case LogLevel.Debug:
                    return TdLogLevel.Debug;
                case LogLevel.Trace:
                    return TdLogLevel.Verbose;
                case LogLevel.None:
                    return TdLogLevel.Fatal;
                default:
                    return TdLogLevel.Info;
            }
        }
    }
}
