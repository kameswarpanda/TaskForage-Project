namespace TaskForge.Domain.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string Action { get; set; } = string.Empty; // INSERT, UPDATE, DELETE
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? PerformedBy { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
}
