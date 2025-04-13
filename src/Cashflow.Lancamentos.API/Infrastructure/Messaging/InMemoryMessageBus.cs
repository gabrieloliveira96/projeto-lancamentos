using Cashflow.Shared.Messaging.Interfaces;
using System.Text.Json;

namespace Cashflow.Lancamentos.API.Infrastructure.Messaging;

public class InMemoryMessageBus : IMessageBus
{
    public Task PublishAsync<T>(T @event, string queue) where T : class
    {
        // Simula envio do evento (pode trocar por RabbitMQ depois)
        Console.WriteLine($"[Mensagem publicada em '{queue}']: {JsonSerializer.Serialize(@event)}");
        return Task.CompletedTask;
    }
}
