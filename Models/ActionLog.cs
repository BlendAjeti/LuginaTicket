namespace LuginaTicket.Models;

public class ActionLog
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // Create, Read, Update, Delete
    public string EntityType { get; set; } = string.Empty; // Movie, Ticket, User, etc.
    public int? EntityId { get; set; }
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
}

