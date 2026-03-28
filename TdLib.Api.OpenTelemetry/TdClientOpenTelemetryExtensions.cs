using System.Diagnostics;
using OpenTelemetry.Trace;
using TdLib.TdApi;

namespace TdLib.Api.OpenTelemetry;

/// <summary>
/// Extension methods for wiring TdSharp OpenTelemetry instrumentation.
/// </summary>
public static class TdClientOpenTelemetryExtensions
{
    /// <summary>
    /// Adds the TdSharp activity source to an OpenTelemetry tracing pipeline.
    /// </summary>
    /// <param name="builder">OpenTelemetry tracer provider builder.</param>
    /// <param name="activitySourceName">Optional custom source name. Defaults to <see cref="TdClientActivitySource.Name"/>.</param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    public static TracerProviderBuilder AddTdSharpInstrumentation(
        this TracerProviderBuilder builder,
        string activitySourceName = TdClientActivitySource.ActivitySourceName)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddSource(activitySourceName);
    }

    /// <summary>
    /// Wraps a TdSharp client with OpenTelemetry activity instrumentation.
    /// </summary>
    /// <param name="client">The client to wrap.</param>
    /// <param name="activitySource">Optional custom activity source. If omitted, a shared default source is used.</param>
    /// <returns>A wrapper that emits activities for every client operation.</returns>
    public static IClient UseOpenTelemetry(
        this TdApi.Client client,
        ActivitySource? activitySource = null)
    {
        ArgumentNullException.ThrowIfNull(client);

        return new ClientOpenTelemetryDecorator(client, activitySource);
    }
}
