using System.Diagnostics;
using System.Text;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Cashflow.Shared.Observability;

public static class MessagingTracingHelper
{
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    public static Activity? StartProducerSpan(ActivitySource source, string name, string? traceParent = null, IDictionary<string, object>? tags = null)
    {
        Activity? activity = string.IsNullOrWhiteSpace(traceParent)
            ? source.StartActivity(name, ActivityKind.Producer)
            : source.StartActivity(name, ActivityKind.Producer, ActivityContext.Parse(traceParent, null));

        if (tags != null)
        {
            foreach (var (key, value) in tags)
                activity?.SetTag(key, value);
        }

        return activity;
    }

    public static void InjectTraceHeaders(Activity? activity, IDictionary<string, object> headers)
    {
        if (activity == null) return;

        Propagator.Inject(
            new PropagationContext(activity.Context, Baggage.Current),
            headers,
            (carrier, key, value) =>
            {
                if (!string.IsNullOrWhiteSpace(value))
                    carrier[key] = System.Text.Encoding.UTF8.GetBytes(value);
            });
    }

    public static Activity? StartConsumerSpan(
        ActivitySource source,
        string name,
        IDictionary<string, object>? headers,
        IDictionary<string, object>? tags = null)
    {
        var parentContext = Propagators.DefaultTextMapPropagator.Extract(
            default,
            headers ?? new Dictionary<string, object>(),
            (carrier, key) =>
            {
                if (carrier.TryGetValue(key, out var value) && value is byte[] bytes)
                    return new[] { Encoding.UTF8.GetString(bytes) };

                return Enumerable.Empty<string>();
            });

        Baggage.Current = parentContext.Baggage;

        var activity = parentContext.ActivityContext.IsValid()
            ? source.StartActivity(name, ActivityKind.Consumer, parentContext.ActivityContext)
            : source.StartActivity(name, ActivityKind.Consumer);

        if (tags != null)
        {
            foreach (var (key, value) in tags)
                activity?.SetTag(key, value);
        }

        return activity;
    }

}