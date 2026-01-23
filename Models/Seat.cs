namespace LuginaTicket.Models;

public class Seat
{
    public int Id { get; set; }
    public int ShowtimeId { get; set; }
    public int CinemaHallId { get; set; }
    public string Row { get; set; } = string.Empty; // A, B, C, etc.
    public int Number { get; set; } // 1, 2, 3, etc.
    public SeatStatus Status { get; set; } = SeatStatus.Available;
    public bool IsWheelchairAccessible { get; set; } = false;
    public bool IsVIP { get; set; } = false;
    
    // Navigation properties
    public virtual Showtime Showtime { get; set; } = null!;
    public virtual CinemaHall CinemaHall { get; set; } = null!;
    public virtual Ticket? Ticket { get; set; }
}

public enum SeatStatus
{
    Available = 0,
    Occupied = 1,
    Selected = 2
}

