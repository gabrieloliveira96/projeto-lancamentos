using System.Diagnostics;
using MediatR;

namespace Cashflow.Shared.Observability
{
    
public class TracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ActivitySource _activitySource;

    public TracingBehavior(ActivitySource activitySource)
    {
        _activitySource = activitySource;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var activityName = $"{typeof(TRequest).Name}";
        using var activity = _activitySource.StartActivity(activityName, ActivityKind.Internal);

        activity?.SetTag("request.type", typeof(TRequest).FullName ?? "unknown");

        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            activity?.SetTag("exception.message", ex.Message);
            activity?.SetTag("exception.stacktrace", ex.StackTrace);
            throw;
        }
    }
}
}
