using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LuginaTicket.Data;
using System.Security.Claims;

namespace LuginaTicket.Controllers;

[Authorize]
public class MyTicketsController : Controller
{
    private readonly ApplicationDbContext _context;

    public MyTicketsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: MyTickets
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        var tickets = await _context.Tickets
            .Include(t => t.Showtime)
                .ThenInclude(s => s.Movie)
            .Include(t => t.Showtime)
                .ThenInclude(s => s.CinemaHall)
            .Include(t => t.Seat)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.PurchaseDate)
            .ToListAsync();

        return View(tickets);
    }

    // GET: MyTickets/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        var ticket = await _context.Tickets
            .Include(t => t.Showtime)
                .ThenInclude(s => s.Movie)
            .Include(t => t.Showtime)
                .ThenInclude(s => s.CinemaHall)
            .Include(t => t.Seat)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (ticket == null)
        {
            return NotFound();
        }

        return View(ticket);
    }
}

