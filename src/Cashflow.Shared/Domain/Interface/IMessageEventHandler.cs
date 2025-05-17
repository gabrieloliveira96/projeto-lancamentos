namespace Cashflow.Shared.Domain.Interface;

public interface IMessageEventHandler<in TEvent>
{
    Task HandleAsync(TEvent @event);
}
