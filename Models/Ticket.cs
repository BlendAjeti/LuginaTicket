namespace LuginaTicket.Models;

public class Ticket
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty; // Unique ticket identifier
    public string UserId { get; set; } = string.Empty;
    public int ShowtimeId { get; set; }
    public int SeatId { get; set; }
    public decimal Price { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Pending;
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public DateTime? ValidatedDate { get; set; }
    public string? Barcode { get; set; }
    
    // Navigation properties
    public virtual ApplicationUser User { get; set; } = null!;
    public virtual Showtime Showtime { get; set; } = null!;
    public virtual Seat Seat { get; set; } = null!;
}

public enum TicketStatus
{
    Pending = 0,
    Confirmed = 1,
    Cancelled = 2,
    Used = 3
}

