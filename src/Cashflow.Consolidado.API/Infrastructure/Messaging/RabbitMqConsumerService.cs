using Cashflow.Shared.Messaging.Events;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using OpenTelemetry.Context.Propagation;
using Cashflow.Consolidado.API.Application.Handlers;
using Cashflow.Shared.Observability;
using OpenTelemetry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Cashflow.Consolidado.API.Infrastructure.Messaging;

public class RabbitMqConsumerService : BackgroundService
{
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    private readonly ILogger<RabbitMqConsumerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ActivitySource _activitySource;

    public RabbitMqConsumerService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<RabbitMqConsumerService> logger,
        ActivitySource activitySource)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
        _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
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
                var headers = ea.BasicProperties?.Headers ?? new Dictionary<string, object>();
                var carrier = new PropagationHeaderCarrier(headers);

                var parentContext = Propagator.Extract(default, carrier, (c, key) =>
                {
                    if (c.TryGetValue(key, out var value) && value is byte[] bytes)
                        return new[] { Encoding.UTF8.GetString(bytes) };

                    return Enumerable.Empty<string>();
                });

                Baggage.Current = parentContext.Baggage;
                
                using var activity = MessagingTracingHelper.StartConsumerSpan(
                    _activitySource,
                    "Consumer.Event",
                    headers,
                    new Dictionary<string, object>
                    {
                        { "messaging.system", "rabbitmq" },
                        { "messaging.destination", _configuration["RabbitMQ:QueueName"] },
                        { "messaging.operation", "receive" }
                    });
                
                _logger.LogInformation("TraceId: {TraceId} | IsValid: {IsValid}",
                    activity?.Context.TraceId, parentContext.ActivityContext.IsValid());

                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var evt = JsonSerializer.Deserialize<LancamentoCriadoEvent>(message);

                if (evt is not null)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var executor = scope.ServiceProvider.GetRequiredService<MessageHandlerExecutor>();
                    await executor.ExecuteAsync(evt, typeof(LancamentoCriadoEvent).Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar evento: {Mensagem}", ex.Message);
            }
        };

        channel.BasicConsume(queue: _configuration["RabbitMQ:QueueName"],
                             autoAck: true,
                             consumer: consumer);

        return Task.CompletedTask;
    }

    private readonly struct PropagationHeaderCarrier : IReadOnlyDictionary<string, object>
    {
        private readonly IDictionary<string, object> _headers;

        public PropagationHeaderCarrier(IDictionary<string, object> headers)
        {
            _headers = headers;
        }

        public object this[string key] => _headers[key];
        public IEnumerable<string> Keys => _headers.Keys;
        public IEnumerable<object> Values => _headers.Values;
        public int Count => _headers.Count;

        public bool ContainsKey(string key) => _headers.ContainsKey(key);
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _headers.GetEnumerator();
        public bool TryGetValue(string key, out object value) => _headers.TryGetValue(key, out value);
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _headers.GetEnumerator();
    }
}
