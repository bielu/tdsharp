using System.Diagnostics;
using TdLib.TdApi;
using TdLib.TdApi.Objects;
using Object = TdLib.TdApi.Object;

namespace TdLib.Api.OpenTelemetry;

public partial class ClientOpenTelemetryDecorator(IClient client, ActivitySource? activitySource = null) : IClient
{
    private Activity? StartCallActivity(string operation, string functionType)
    {
        var activity = activitySource.StartActivity($"tdclient.{operation}", ActivityKind.Client);
        activity?.SetTag("td.operation", operation);
        activity?.SetTag("td.function_type", functionType);
        return activity;
    }

    private static void MarkAsError(Activity? activity, Exception ex)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity.SetTag("error.type", ex.GetType().FullName);
        activity.SetTag("error.message", ex.Message);
    }

    public event EventHandler<Update>? UpdateReceived;

    public void Send<TResut>(Function<TResut> function)
    {
        ArgumentNullException.ThrowIfNull(function);

        using var activity = StartCallActivity("send", function.GetType().FullName ?? "send");

        try
        {
            client.Send(function);
        }
        catch (Exception ex)
        {
            MarkAsError(activity, ex);
            throw;
        }
    }

    public TResult Execute<TResult>(Function<TResult> function) where TResult : Object
    {
        ArgumentNullException.ThrowIfNull(function);

        using var activity = StartCallActivity("Execute", function.GetType().FullName ?? "Execute");

        try
        {
            return client.Execute(function);
        }
        catch (Exception ex)
        {
            MarkAsError(activity, ex);
            throw;
        }
    }

    public Task<TResult> ExecuteAsync<TResult>(Function<TResult> function) where TResult : Object
    {
        ArgumentNullException.ThrowIfNull(function);

        using var activity = StartCallActivity("ExecuteAsync", function.GetType().FullName ?? "ExecuteAsync");

        try
        {
            return client.ExecuteAsync(function);
        }
        catch (Exception ex)
        {
            MarkAsError(activity, ex);
            throw;
        }
    }
}
