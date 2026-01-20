using Microsoft.AspNetCore.Mvc;
using LuginaTicket.Data;
using Microsoft.EntityFrameworkCore;

namespace LuginaTicket.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var movies = await _context.Movies
            .Where(m => m.IsActive)
            .OrderByDescending(m => m.ReleaseDate)
            .Take(8)
            .ToListAsync();

        return View(movies);
    }

    public IActionResult Privacy()
    {
        return View();
    }
}

