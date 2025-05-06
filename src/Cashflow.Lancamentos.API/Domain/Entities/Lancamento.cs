using Cashflow.Shared.Domain.Enums;

namespace Cashflow.Lancamentos.API.Domain.Entities;

public class Lancamento
{
    public Guid Id { get; set; }
    public DateTime Data { get; set; }
    public decimal Valor { get; set; }
    public TipoLancamento Tipo { get; set; }
    public string Descricao { get; set; } = string.Empty;
    
    public static Lancamento Create(DateTime data, decimal valor, TipoLancamento tipo, string descricao)
    {
        if (valor <= 0)
            throw new ArgumentException("O valor do lançamento deve ser maior que 0");

        return new Lancamento
        {
            Id = Guid.NewGuid(),
            Data = data,
            Valor = valor,
            Tipo = tipo,
            Descricao = descricao
        };
    }

}

