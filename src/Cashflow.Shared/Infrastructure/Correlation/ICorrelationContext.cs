namespace Cashflow.Shared.Infrastructure.Correlation;

public interface ICorrelationContext
{
    string CorrelationId { get; set; }
}
