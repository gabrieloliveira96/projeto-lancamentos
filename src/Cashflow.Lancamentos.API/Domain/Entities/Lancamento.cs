using Cashflow.Shared.Domain.Enums;

namespace Cashflow.Lancamentos.API.Domain.Entities;

public class Lancamento
{
    public Guid Id { get; set; }
    public DateTime Data { get; set; }
    public decimal Valor { get; set; }
    public TipoLancamento Tipo { get; set; }
    public string Descricao { get; set; } = string.Empty;
}
