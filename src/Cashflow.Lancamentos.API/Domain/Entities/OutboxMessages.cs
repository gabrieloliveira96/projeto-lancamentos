public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public bool Processed { get; set; }
    public DateTime? ProcessedAt { get; set; }
    
    public static OutboxMessage Create(string type, string content)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            Processed = false,
            ProcessedAt = null
        };
    }
}
