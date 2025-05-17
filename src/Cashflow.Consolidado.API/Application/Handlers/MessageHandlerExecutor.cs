using System.Diagnostics;
using Cashflow.Shared.Domain.Interface;

namespace Cashflow.Consolidado.API.Application.Handlers;

public class MessageHandlerExecutor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ActivitySource _activitySource;

    public MessageHandlerExecutor(IServiceProvider serviceProvider, ActivitySource activitySource)
    {
        _serviceProvider = serviceProvider;
        _activitySource = activitySource;
    }

    public async Task ExecuteAsync<T>(T @event, string spanName) where T : class
    {
        using var scope = _serviceProvider.CreateScope();
        using var activity = _activitySource.StartActivity($"Handle.{spanName}", ActivityKind.Internal);

        activity?.SetTag("event.type", typeof(T).Name);
        activity?.SetTag("event.source", "mensageria");

        var handler = scope.ServiceProvider.GetService<IMessageEventHandler<T>>();
        if (handler is null)
            throw new InvalidOperationException($"Handler não registrado para o tipo {typeof(T).Name}");

        await handler.HandleAsync(@event);
    }
}
