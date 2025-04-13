namespace Cashflow.Consolidado.API.Domain.Aggregates;

public class SaldoConsolidado
{
    public Guid Id { get; set; }
    public DateTime Data { get; set; }
    public decimal Saldo { get; set; }
}
