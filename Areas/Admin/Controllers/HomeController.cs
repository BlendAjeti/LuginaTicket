using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using LuginaTicket.Data;
using LuginaTicket.Models;
using LuginaTicket.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LuginaTicket.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var stats = new DashboardViewModel
        {
            TotalUsers = await _context.Users.CountAsync(),
            TotalMovies = await _context.Movies.CountAsync(),
            TotalTickets = await _context.Tickets.CountAsync(),
            TotalRevenue = await _context.Tickets
                .Where(t => t.Status == TicketStatus.Confirmed)
                .SumAsync(t => t.Price),
            RecentTickets = await _context.Tickets
                .Include(t => t.User)
                .Include(t => t.Showtime)
                    .ThenInclude(s => s.Movie)
                .OrderByDescending(t => t.PurchaseDate)
                .Take(10)
                .ToListAsync(),
            MoviesByGenre = await _context.Movies
                .Where(m => m.IsActive)
                .GroupBy(m => m.Genre)
                .Select(g => new GenreCount { Genre = g.Key, Count = g.Count() })
                .ToListAsync()
        };

        return View(stats);
    }
}

