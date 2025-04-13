using Cashflow.Lancamentos.API.Infrastructure.Persistence;
using Cashflow.Shared.Messaging.Events;
using Cashflow.Shared.Messaging.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json;

public class OutboxMessageDispatcher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxMessageDispatcher> _logger;

    public OutboxMessageDispatcher(IServiceProvider serviceProvider, ILogger<OutboxMessageDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Iniciando dispatcher de Outbox...");

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LancamentosDbContext>();
            var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

            var mensagens = await context.OutboxMessages
                .Where(m => !m.Processed)
                .OrderBy(m => m.CreatedAt)
                .Take(10)
                .ToListAsync(stoppingToken);

            foreach (var msg in mensagens)
            {
                try
                {
                    switch (msg.Type)
                    {
                        case nameof(LancamentoCriadoEvent):
                            var evento = JsonSerializer.Deserialize<LancamentoCriadoEvent>(msg.Content);
                            if (evento != null)
                            {
                                _logger.LogInformation("Publicando evento: {Id} | Tipo: {Tipo} | CorrelationId: {CorrelationId}",
                                    evento.Id, msg.Type, evento.CorrelationId);

                                await messageBus.PublishAsync(evento, "cashflow.events");
                            }
                            break;

                        default:
                            _logger.LogWarning("Tipo de evento desconhecido: {Tipo}", msg.Type);
                            continue;
                    }

                    msg.Processed = true;
                    context.OutboxMessages.Update(msg);
                    await context.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar mensagem Outbox: {Mensagem}", msg);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
