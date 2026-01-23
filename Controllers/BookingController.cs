using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using LuginaTicket.Data;
using LuginaTicket.Models;
using LuginaTicket.Services;
using LuginaTicket.ViewModels;
using System.Security.Claims;

namespace LuginaTicket.Controllers;

[Authorize]
public class BookingController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IActionLogService _actionLogService;

    public BookingController(ApplicationDbContext context, IActionLogService actionLogService)
    {
        _context = context;
        _actionLogService = actionLogService;
    }

    // GET: Booking/SelectSeats/5
    public async Task<IActionResult> SelectSeats(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var showtime = await _context.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.CinemaHall)
            .Include(s => s.Seats)
                .ThenInclude(seat => seat.Ticket)
            .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

        if (showtime == null)
        {
            return NotFound();
        }

        // Mark seats as occupied if they have tickets
        foreach (var seat in showtime.Seats)
        {
            if (seat.Ticket != null && seat.Ticket.Status == TicketStatus.Confirmed)
            {
                seat.Status = SeatStatus.Occupied;
            }
        }

        return View(showtime);
    }

    // POST: Booking/ReserveSeats
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReserveSeats(int showtimeId, string[] selectedSeats)
    {
        if (selectedSeats == null || selectedSeats.Length == 0)
        {
            return Json(new { success = false, message = "Please select at least one seat." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Json(new { success = false, message = "User not found." });
        }

        var showtime = await _context.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Seats)
            .FirstOrDefaultAsync(s => s.Id == showtimeId && s.IsActive);

        if (showtime == null)
        {
            return Json(new { success = false, message = "Showtime not found." });
        }

        // Store selected seats in session
        var seatIds = new List<int>();
        foreach (var seatInfo in selectedSeats)
        {
            var parts = seatInfo.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out int seatNumber))
            {
                var seat = showtime.Seats.FirstOrDefault(s => s.Row == parts[0] && s.Number == seatNumber);
                if (seat != null && seat.Status == SeatStatus.Available)
                {
                    seatIds.Add(seat.Id);
                }
            }
        }

        if (seatIds.Count == 0)
        {
            return Json(new { success = false, message = "No valid seats selected." });
        }

        HttpContext.Session.SetString("SelectedSeats", string.Join(",", seatIds));
        HttpContext.Session.SetInt32("ShowtimeId", showtimeId);

        return Json(new { success = true, seatCount = seatIds.Count, totalPrice = seatIds.Count * showtime.Price });
    }

    // GET: Booking/Payment
    public async Task<IActionResult> Payment()
    {
        var showtimeId = HttpContext.Session.GetInt32("ShowtimeId");
        var selectedSeatsStr = HttpContext.Session.GetString("SelectedSeats");

        if (showtimeId == null || string.IsNullOrEmpty(selectedSeatsStr))
        {
            return RedirectToAction("Index", "Movies");
        }

        var seatIds = selectedSeatsStr.Split(',').Select(int.Parse).ToList();

        var showtime = await _context.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.CinemaHall)
            .FirstOrDefaultAsync(s => s.Id == showtimeId);

        if (showtime == null)
        {
            return NotFound();
        }

        var seats = await _context.Seats
            .Where(s => seatIds.Contains(s.Id))
            .ToListAsync();

        ViewBag.Seats = seats;
        ViewBag.TotalPrice = seats.Count * showtime.Price;

        return View(showtime);
    }

    // POST: Booking/ProcessPayment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessPayment(PaymentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Payment", model);
        }

        // Validate card number (16 digits)
        var cardNumber = model.CardNumber?.Replace(" ", "").Replace("-", "");
        if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length != 16 || !cardNumber.All(char.IsDigit))
        {
            ModelState.AddModelError("CardNumber", "Card number must be 16 digits.");
            return View("Payment", model);
        }

        // Validate expiry date
        if (model.ExpiryDate < DateTime.Now)
        {
            ModelState.AddModelError("ExpiryDate", "Card has expired.");
            return View("Payment", model);
        }

        var showtimeId = HttpContext.Session.GetInt32("ShowtimeId");
        var selectedSeatsStr = HttpContext.Session.GetString("SelectedSeats");

        if (showtimeId == null || string.IsNullOrEmpty(selectedSeatsStr))
        {
            return RedirectToAction("Index", "Movies");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        var seatIds = selectedSeatsStr.Split(',').Select(int.Parse).ToList();

        var showtime = await _context.Showtimes
            .Include(s => s.Movie)
            .FirstOrDefaultAsync(s => s.Id == showtimeId);

        if (showtime == null)
        {
            return NotFound();
        }

        var seats = await _context.Seats
            .Where(s => seatIds.Contains(s.Id) && s.Status == SeatStatus.Available)
            .ToListAsync();

        if (seats.Count != seatIds.Count)
        {
            return Json(new { success = false, message = "Some seats are no longer available." });
        }

        // Create tickets
        var tickets = new List<Ticket>();
        foreach (var seat in seats)
        {
            var ticketNumber = GenerateTicketNumber();
            var ticket = new Ticket
            {
                TicketNumber = ticketNumber,
                UserId = userId,
                ShowtimeId = showtime.Id,
                SeatId = seat.Id,
                Price = showtime.Price,
                Status = TicketStatus.Confirmed,
                Barcode = GenerateBarcode()
            };

            tickets.Add(ticket);
            seat.Status = SeatStatus.Occupied;
        }

        _context.Tickets.AddRange(tickets);
        await _context.SaveChangesAsync();

        // Log action
        await _actionLogService.LogActionAsync(userId, "Create", "Ticket", null, 
            $"Created {tickets.Count} tickets for showtime {showtimeId}");

        // Clear session
        HttpContext.Session.Remove("SelectedSeats");
        HttpContext.Session.Remove("ShowtimeId");

        return RedirectToAction("TicketConfirmation", new { ticketIds = string.Join(",", tickets.Select(t => t.Id)) });
    }

    // GET: Booking/TicketConfirmation
    public async Task<IActionResult> TicketConfirmation(string ticketIds)
    {
        if (string.IsNullOrEmpty(ticketIds))
        {
            return NotFound();
        }

        var ids = ticketIds.Split(',').Select(int.Parse).ToList();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var tickets = await _context.Tickets
            .Include(t => t.Showtime)
                .ThenInclude(s => s.Movie)
            .Include(t => t.Showtime)
                .ThenInclude(s => s.CinemaHall)
            .Include(t => t.Seat)
            .Where(t => ids.Contains(t.Id) && t.UserId == userId)
            .ToListAsync();

        if (tickets.Count == 0)
        {
            return NotFound();
        }

        return View(tickets);
    }

    private string GenerateTicketNumber()
    {
        return $"TKT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
    }

    private string GenerateBarcode()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 20).ToUpper();
    }
}

