using Cashflow.Shared.Domain.Enums;

namespace Cashflow.Shared.Messaging.Events;

public class LancamentoCriadoEvent :BaseMessage
{
    public Guid Id { get; set; }
    public DateTime Data { get; set; }
    public decimal Valor { get; set; }
    public TipoLancamento Tipo { get; set; }
    public string Descricao { get; set; } = string.Empty;
}
