namespace Cashflow.Shared.Messaging;

public abstract class BaseMessage
{
    public string? CorrelationId { get; set; }
}
