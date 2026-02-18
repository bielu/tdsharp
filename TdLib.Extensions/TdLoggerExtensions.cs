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
    /// Provides extension methods for integrating TDLib logging with Microsoft.Extensions.Logging
    /// </summary>
    public static class TdLoggerExtensions
    {
        private static ILogger _logger;
        private static Callback _nativeCallback;

        /// <summary>
        /// Configures TDLib to use the specified ILogger for logging fatal errors.
        /// Note: TDLib native bindings only support fatal error callbacks by default.
        /// For full logging integration, use UseTdLibLogging with log file monitoring.
        /// </summary>
        /// <param name="client">The TdClient instance</param>
        /// <param name="logger">The ILogger to use for logging</param>
        /// <param name="logLevel">The TDLib log level to set</param>
        public static void UseTdLibLogging(this TdClient client, ILogger logger, TdLogLevel logLevel = TdLogLevel.Warning)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            _logger = logger;

            // Set the verbosity level
            client.Bindings.SetLogVerbosityLevel(logLevel);

            // Set up the fatal error callback to route to ILogger
            _nativeCallback = OnFatalError;
            client.Bindings.SetLogFatalErrorCallback(_nativeCallback);
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
            if (_logger == null || messagePtr == IntPtr.Zero)
                return;

            try
            {
                var message = Marshal.PtrToStringAnsi(messagePtr);
                _logger.LogCritical("[TDLib Fatal] {Message}", message);
            }
            catch (Exception ex)
            {
                // Swallow exceptions in callback to prevent native crashes
                System.Diagnostics.Debug.WriteLine($"Error in TDLib fatal error callback: {ex}");
            }
        }

        /// <summary>
        /// Maps TDLib verbosity level to Microsoft.Extensions.Logging LogLevel
        /// </summary>
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
                case TdLogLevel.All:
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
