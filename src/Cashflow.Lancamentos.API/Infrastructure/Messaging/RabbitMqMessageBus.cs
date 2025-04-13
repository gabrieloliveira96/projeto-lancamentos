using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Cashflow.Shared.Messaging.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cashflow.Lancamentos.API.Infrastructure.Messaging;

public class RabbitMqMessageBus : IMessageBus
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqMessageBus> _logger;

    public RabbitMqMessageBus(IConfiguration configuration, ILogger<RabbitMqMessageBus> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task PublishAsync<T>(T @event, string queue) where T : class
    {
        try
        {
            var factory = new ConnectionFactory()
            {
                HostName = _configuration["RabbitMQ:Host"],
                UserName = _configuration["RabbitMQ:Username"],
                Password = _configuration["RabbitMQ:Password"],
                Port = int.Parse(_configuration["RabbitMQ:Port"])
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var payload = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(payload);

            channel.BasicPublish(exchange: "", routingKey: queue, basicProperties: null, body: body);

            _logger.LogInformation("Evento publicado com sucesso na fila {Queue}. Payload: {Payload}", queue, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao publicar mensagem na fila {Queue}", queue);
            throw;
        }

        return Task.CompletedTask;
    }
}
