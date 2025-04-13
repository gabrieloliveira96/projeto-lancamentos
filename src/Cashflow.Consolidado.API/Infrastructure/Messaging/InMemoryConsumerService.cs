using Cashflow.Shared.Messaging.Events;
using Cashflow.Consolidado.API.Application.Handlers;
using Cashflow.Shared.Domain.Enums;

namespace Cashflow.Consolidado.API.Infrastructure.Messaging;

public class InMemoryConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public InMemoryConsumerService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Simulação do consumidor em fila (troque por RabbitMQ depois)
        while (!stoppingToken.IsCancellationRequested)
        {
            // Simulação: imagine que um evento chegou
            var evt = new LancamentoCriadoEvent
            {
                Id = Guid.NewGuid(),
                Data = DateTime.Today,
                Valor = 100,
                Tipo = TipoLancamento.Credito,
                Descricao = "Evento simulado"
            };

            using var scope = _serviceProvider.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ProcessLancamentoEventHandler>();
            await handler.HandleAsync(evt);

            await Task.Delay(10000, stoppingToken); // simula recebimento periódico
        }
    }
}
