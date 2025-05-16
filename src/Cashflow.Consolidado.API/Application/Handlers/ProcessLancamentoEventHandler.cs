using System.Diagnostics;
using Cashflow.Consolidado.API.Domain.Aggregates;
using Cashflow.Consolidado.API.Infrastructure.Persistence;
using Cashflow.Consolidado.API.Observability;
using Cashflow.Shared.Messaging.Events;
using Cashflow.Shared.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cashflow.Consolidado.API.Application.Handlers;

public class ProcessLancamentoEventHandler
{
    private readonly ConsolidadoDbContext _context;
    private readonly ILogger<ProcessLancamentoEventHandler> _logger;

    public ProcessLancamentoEventHandler(ConsolidadoDbContext context, ILogger<ProcessLancamentoEventHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task HandleAsync(LancamentoCriadoEvent evt)
    {
        try
        {
            using var activity = Tracing.Source.StartActivity("ProcessLancamentoEventHandler", ActivityKind.Consumer);
            activity?.SetTag("evento.tipo", "LancamentoCriadoEvent");
        _logger.LogInformation(
            "Evento recebido: {EventoId} | Tipo: {Tipo} | Valor: {Valor} | Data: {Data} | CorrelationId: {CorrelationId}",
            evt.Id, evt.Tipo, evt.Valor, evt.Data, evt.CorrelationId);


            var saldo = await _context.SaldosConsolidados
                .FirstOrDefaultAsync(s => s.Data.Date == evt.Data.Date);

            var valorAjustado = evt.Tipo == TipoLancamento.Credito
                ? evt.Valor
                : -evt.Valor;

            if (saldo == null)
            {
                saldo = new SaldoConsolidado
                {
                    Id = Guid.NewGuid(),
                    Data = evt.Data.Date,
                    Saldo = valorAjustado
                };

                _context.SaldosConsolidados.Add(saldo);
                _logger.LogInformation("Novo saldo criado para {Data}: {Valor}", evt.Data.Date, valorAjustado);
            }
            else
            {
                saldo.Saldo += valorAjustado;
                _logger.LogInformation("Saldo atualizado para {Data}: {NovoSaldo}", evt.Data.Date, saldo.Saldo);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar evento de lançamento: {@Evento}", evt);
            throw;
        }
    }
}
