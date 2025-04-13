using Cashflow.Shared.Domain.Enums;
using MediatR;
using System;

namespace Cashflow.Lancamentos.API.Application.Commands.Lancamentos.CreateLancamento;

public class CreateLancamentoCommand : IRequest<Guid>
{
    public DateTime Data { get; set; }
    public decimal Valor { get; set; }
    public TipoLancamento Tipo { get; set; }
    public string Descricao { get; set; } = string.Empty;
}
