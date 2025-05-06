using Cashflow.Shared.Domain.Enums;

namespace Cashflow.Shared.Messaging.Events;

public class LancamentoCriadoEvent :BaseMessage
{
    public Guid Id { get; set; }
    public DateTime Data { get; set; }
    public decimal Valor { get; set; }
    public TipoLancamento Tipo { get; set; }
    public string Descricao { get; set; } = string.Empty;
    
    public static LancamentoCriadoEvent Create(Guid id, DateTime data, decimal valor, TipoLancamento tipo, string descricao,string correlationId)
    {
        return new LancamentoCriadoEvent
        {
            Id = id,
            Data = data,
            Valor = valor,
            Tipo = tipo,
            Descricao = descricao,
            CorrelationId = correlationId
        };
    }
}
