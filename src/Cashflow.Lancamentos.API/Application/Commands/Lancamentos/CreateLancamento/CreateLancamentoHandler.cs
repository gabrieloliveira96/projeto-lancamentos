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
            var lancamento = new Lancamento
            {
                Id = Guid.NewGuid(),
                Data = request.Data,
                Valor = request.Valor,
                Tipo = request.Tipo,
                Descricao = request.Descricao
            };

            _context.Lancamentos.Add(lancamento);

            var evento = new LancamentoCriadoEvent
            {
                Id = lancamento.Id,
                Data = lancamento.Data,
                Valor = lancamento.Valor,
                Tipo = lancamento.Tipo,
                Descricao = lancamento.Descricao,
                CorrelationId = _correlation.CorrelationId
            };

            _context.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = nameof(LancamentoCriadoEvent),
                Content = JsonSerializer.Serialize(evento),
                CreatedAt = DateTime.UtcNow,
                Processed = false
            });

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
