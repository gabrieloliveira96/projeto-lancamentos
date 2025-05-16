public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "";
    public string Content { get; set; } = "";
    public string? TraceParent { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Processed { get; set; }

    public static OutboxMessage Create(string type, string content, string? traceParent = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Type = type,
            Content = content,
            TraceParent = traceParent,
            CreatedAt = DateTime.UtcNow,
            Processed = false
        };
}
