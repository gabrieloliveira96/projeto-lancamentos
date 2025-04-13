namespace Cashflow.Shared.Infrastructure.Correlation;

public class CorrelationContext : ICorrelationContext
{
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
}
