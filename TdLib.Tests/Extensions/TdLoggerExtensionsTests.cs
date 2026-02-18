// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using TdLib.Bindings;
using TdLib.Extensions;
using Xunit;

namespace TdLib.Tests.Extensions
{
    public class TdLoggerExtensionsTests
    {
        [Theory]
        [InlineData(TdLogLevel.Fatal, LogLevel.Critical)]
        [InlineData(TdLogLevel.Error, LogLevel.Error)]
        [InlineData(TdLogLevel.Warning, LogLevel.Warning)]
        [InlineData(TdLogLevel.Info, LogLevel.Information)]
        [InlineData(TdLogLevel.Debug, LogLevel.Debug)]
        [InlineData(TdLogLevel.Verbose, LogLevel.Trace)]
        [InlineData(TdLogLevel.All, LogLevel.Trace)]
        public void TdLogLevel_ToLogLevel_MapsCorrectly(TdLogLevel tdLogLevel, LogLevel expectedLogLevel)
        {
            var result = tdLogLevel.ToLogLevel();
            Assert.Equal(expectedLogLevel, result);
        }

        [Theory]
        [InlineData(0, LogLevel.Critical)]
        [InlineData(1, LogLevel.Error)]
        [InlineData(2, LogLevel.Warning)]
        [InlineData(3, LogLevel.Information)]
        [InlineData(4, LogLevel.Debug)]
        [InlineData(5, LogLevel.Trace)]
        [InlineData(100, LogLevel.Trace)]
        public void VerbosityLevel_ToLogLevel_MapsCorrectly(int verbosityLevel, LogLevel expectedLogLevel)
        {
            var result = TdLoggerExtensions.ToLogLevel(verbosityLevel);
            Assert.Equal(expectedLogLevel, result);
        }

        [Theory]
        [InlineData(LogLevel.Critical, TdLogLevel.Fatal)]
        [InlineData(LogLevel.Error, TdLogLevel.Error)]
        [InlineData(LogLevel.Warning, TdLogLevel.Warning)]
        [InlineData(LogLevel.Information, TdLogLevel.Info)]
        [InlineData(LogLevel.Debug, TdLogLevel.Debug)]
        [InlineData(LogLevel.Trace, TdLogLevel.Verbose)]
        [InlineData(LogLevel.None, TdLogLevel.Fatal)]
        public void LogLevel_ToTdLogLevel_MapsCorrectly(LogLevel logLevel, TdLogLevel expectedTdLogLevel)
        {
            var result = logLevel.ToTdLogLevel();
            Assert.Equal(expectedTdLogLevel, result);
        }
    }

    public class TdLibLoggerTests
    {
        [Fact]
        public void TdLibLogger_Constructor_ThrowsOnNullCategoryName()
        {
            using (var client = new TdClient())
            {
                Assert.Throws<ArgumentNullException>(() => new TdLibLogger(null, client));
            }
        }

        [Fact]
        public void TdLibLogger_Constructor_ThrowsOnNullClient()
        {
            Assert.Throws<ArgumentNullException>(() => new TdLibLogger("TestCategory", null));
        }

        [Fact]
        public void TdLibLogger_IsEnabled_ReturnsFalseForNone()
        {
            using (var client = new TdClient())
            {
                var logger = new TdLibLogger("TestCategory", client);
                Assert.False(logger.IsEnabled(LogLevel.None));
            }
        }

        [Fact]
        public void TdLibLogger_IsEnabled_ReturnsTrueForEnabledLevels()
        {
            using (var client = new TdClient())
            {
                var logger = new TdLibLogger("TestCategory", client, TdLogLevel.Info);
                Assert.True(logger.IsEnabled(LogLevel.Critical));
                Assert.True(logger.IsEnabled(LogLevel.Error));
                Assert.True(logger.IsEnabled(LogLevel.Warning));
                Assert.True(logger.IsEnabled(LogLevel.Information));
            }
        }

        [Fact]
        public void TdLibLogger_BeginScope_ReturnsNonNullDisposable()
        {
            using (var client = new TdClient())
            {
                var logger = new TdLibLogger("TestCategory", client);
                var scope = logger.BeginScope("test scope");
                Assert.NotNull(scope);
                scope.Dispose(); // Should not throw
            }
        }
    }

    public class TdLibLoggerProviderTests
    {
        [Fact]
        public void TdLibLoggerProvider_Constructor_ThrowsOnNullClient()
        {
            Assert.Throws<ArgumentNullException>(() => new TdLibLoggerProvider(null));
        }

        [Fact]
        public void TdLibLoggerProvider_CreateLogger_ReturnsLogger()
        {
            using (var client = new TdClient())
            using (var provider = new TdLibLoggerProvider(client))
            {
                var logger = provider.CreateLogger("TestCategory");
                Assert.NotNull(logger);
                Assert.IsType<TdLibLogger>(logger);
            }
        }

        [Fact]
        public void TdLibLoggerProvider_Dispose_CanBeCalledMultipleTimes()
        {
            using (var client = new TdClient())
            {
                var provider = new TdLibLoggerProvider(client);
                provider.Dispose();
                provider.Dispose(); // Should not throw
            }
        }
    }

    public class TdLibLoggerProviderExtensionsTests
    {
        [Fact]
        public void AddTdLib_ThrowsOnNullFactory()
        {
            using (var client = new TdClient())
            {
                Assert.Throws<ArgumentNullException>(() => 
                    TdLibLoggerProviderExtensions.AddTdLib(null, client));
            }
        }
    }

    /// <summary>
    /// A simple test logger that captures log messages for verification
    /// </summary>
    internal class TestLogger : ILogger
    {
        public List<(LogLevel Level, string Message)> LoggedMessages { get; } = new List<(LogLevel, string)>();

        public IDisposable BeginScope<TState>(TState state) => new NullScope();

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, exception);
            LoggedMessages.Add((logLevel, message));
        }

        private class NullScope : IDisposable
        {
            public void Dispose() { }
        }
    }
}
