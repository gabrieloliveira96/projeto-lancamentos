public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public bool Processed { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
