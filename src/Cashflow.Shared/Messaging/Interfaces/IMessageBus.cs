using System.Threading.Tasks;

namespace Cashflow.Shared.Messaging.Interfaces;

public interface IMessageBus
{
    Task PublishAsync<T>(T @event, string queue) where T : class;
}
