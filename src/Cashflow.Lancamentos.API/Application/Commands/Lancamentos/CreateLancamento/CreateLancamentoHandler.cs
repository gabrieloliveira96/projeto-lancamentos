using Cashflow.Lancamentos.API.Application.Commands.Lancamentos.CreateLancamento;
using Cashflow.Lancamentos.API.Domain.Entities;
using Cashflow.Lancamentos.API.Infrastructure.Persistence;
using Cashflow.Shared.Messaging.Events;
using MediatR;
using Cashflow.Shared.Infrastructure.Correlation;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class CreateLancamentoHandler : IRequestHandler<CreateLancamentoCommand, Guid>
{
    private readonly LancamentosDbContext _context;
    private readonly ILogger<CreateLancamentoHandler> _logger;
    private readonly ICorrelationContext _correlation;

    public CreateLancamentoHandler(
        LancamentosDbContext context,
        ILogger<CreateLancamentoHandler> logger,
        ICorrelationContext correlation)
    {
        _context = context;
        _logger = logger;
        _correlation = correlation;
    }

    public async Task<Guid> Handle(CreateLancamentoCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando criação do lançamento: {@Request}", request);

        try
        {
            var lancamento = Lancamento.Create(
                request.Data,
                request.Valor,
                request.Tipo,
                request.Descricao
            );

            _context.Lancamentos.Add(lancamento);

            var evento = LancamentoCriadoEvent.Create(
                lancamento.Id,
                lancamento.Data,
                lancamento.Valor,
                lancamento.Tipo,
                lancamento.Descricao,
                _correlation.CorrelationId
            );
            
            _context.OutboxMessages.Add(OutboxMessage.Create(
                nameof(LancamentoCriadoEvent),
                JsonSerializer.Serialize(evento)));

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Lançamento e evento registrados com sucesso. ID: {Id}", lancamento.Id);

            return lancamento.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar lançamento. Dados: {@Request}", request);
            throw;
        }
    }
}
