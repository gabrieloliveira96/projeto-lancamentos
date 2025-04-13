using Cashflow.Shared.Messaging.Events;
using System.Text;
using System.Text.Json;
using Cashflow.Consolidado.API.Application.Handlers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Cashflow.Consolidado.API.Infrastructure.Messaging;

public class RabbitMqConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public RabbitMqConsumerService(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory()
        {
            HostName = _configuration["RabbitMQ:Host"],
            UserName = _configuration["RabbitMQ:Username"],
            Password = _configuration["RabbitMQ:Password"],
            Port = int.Parse(_configuration["RabbitMQ:Port"])
        };

        var connection = factory.CreateConnection();
        var channel = connection.CreateModel();

        channel.QueueDeclare(queue: _configuration["RabbitMQ:QueueName"],
                             durable: true,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var evt = JsonSerializer.Deserialize<LancamentoCriadoEvent>(message);

                if (evt is not null)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<ProcessLancamentoEventHandler>();
                    await handler.HandleAsync(evt);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar evento: {ex.Message}");
            }
        };

        channel.BasicConsume(queue: _configuration["RabbitMQ:QueueName"],
                             autoAck: true,
                             consumer: consumer);

        return Task.CompletedTask;
    }
}
