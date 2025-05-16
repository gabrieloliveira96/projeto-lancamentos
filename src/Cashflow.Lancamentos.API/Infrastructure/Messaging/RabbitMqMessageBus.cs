using OpenTelemetry.Context.Propagation;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Cashflow.Shared.Messaging.Interfaces;
using OpenTelemetry;
using RabbitMQ.Client;

public class RabbitMqMessageBus : IMessageBus
{
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;
    private readonly IConfiguration _config;
    private readonly ILogger<RabbitMqMessageBus> _logger;

    public RabbitMqMessageBus(IConfiguration config, ILogger<RabbitMqMessageBus> logger)
    {
        _config = config;
        _logger = logger;
    }

    public Task PublishAsync<T>(T @event, string queue) where T : class
    {
        var factory = new ConnectionFactory
        {
            HostName = _config["RabbitMQ:Host"],
            UserName = _config["RabbitMQ:Username"],
            Password = _config["RabbitMQ:Password"],
            Port = int.Parse(_config["RabbitMQ:Port"])
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false, arguments: null);

        var context = Activity.Current?.Context ?? default;

        _logger.LogInformation("PublishAsync TraceId: {TraceId}", context.TraceId);

        var props = channel.CreateBasicProperties();
        props.Headers = new Dictionary<string, object>();

        Propagator.Inject(new PropagationContext(context, Baggage.Current), props.Headers,
            (dict, key, value) =>
            {
                if (!string.IsNullOrWhiteSpace(value))
                    dict[key] = Encoding.UTF8.GetBytes(value);
            });


        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event));

        channel.BasicPublish(exchange: "", routingKey: queue, basicProperties: props, body: body);

        _logger.LogInformation("Evento publicado com sucesso na fila {Queue}. TraceId={TraceId}", queue, context.TraceId);
        return Task.CompletedTask;
    }
}
