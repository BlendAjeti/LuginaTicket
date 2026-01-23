namespace LuginaTicket.Models;

public class Showtime
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public int CinemaHallId { get; set; }
    public DateTime ShowDateTime { get; set; }
    public string ViewType { get; set; } = "2D"; // 2D, 3D, IMAX
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Movie Movie { get; set; } = null!;
    public virtual CinemaHall CinemaHall { get; set; } = null!;
    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}

