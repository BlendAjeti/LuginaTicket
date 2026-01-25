using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LuginaTicket.Data;
using LuginaTicket.Models;
using Microsoft.AspNetCore.Authorization;
using LuginaTicket.Services;

namespace LuginaTicket.Controllers;

public class MoviesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IActionLogService _actionLogService;

    public MoviesController(ApplicationDbContext context, IActionLogService actionLogService)
    {
        _context = context;
        _actionLogService = actionLogService;
    }

    // GET: Movies
    public async Task<IActionResult> Index(string? search, string? genre, string? sortBy, int page = 1, int pageSize = 8)
    {
        var query = _context.Movies.Where(m => m.IsActive).AsQueryable();

        // Search filter - search by title and genre
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(m => m.Title.Contains(search) || m.Genre.Contains(search));
        }

        // Genre filter
        if (!string.IsNullOrEmpty(genre))
        {
            query = query.Where(m => m.Genre.Contains(genre));
        }

        // Sorting
        query = sortBy switch
        {
            "title_asc" => query.OrderBy(m => m.Title),
            "title_desc" => query.OrderByDescending(m => m.Title),
            "date_asc" => query.OrderBy(m => m.ReleaseDate),
            "date_desc" => query.OrderByDescending(m => m.ReleaseDate),
            _ => query.OrderByDescending(m => m.ReleaseDate)
        };

        var totalCount = await query.CountAsync();
        var movies = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.Genre = genre;
        ViewBag.SortBy = sortBy;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        ViewBag.TotalCount = totalCount;

        // Get unique genres for filter dropdown
        ViewBag.Genres = await _context.Movies
            .Where(m => m.IsActive)
            .Select(m => m.Genre)
            .Distinct()
            .ToListAsync();

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = _context.Users.FirstOrDefault(u => u.UserName == User.Identity.Name)?.Id;
            if (userId != null)
            {
                await _actionLogService.LogActionAsync(userId, "Read", "Movie", null, "Viewed movies list");
            }
        }

        return View(movies);
    }

    // GET: Movies/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var movie = await _context.Movies
            .Include(m => m.Showtimes)
                .ThenInclude(s => s.CinemaHall)
            .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

        if (movie == null)
        {
            return NotFound();
        }

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = _context.Users.FirstOrDefault(u => u.UserName == User.Identity.Name)?.Id;
            if (userId != null)
            {
                await _actionLogService.LogActionAsync(userId, "Read", "Movie", id, $"Viewed movie details: {movie.Title}");
            }
        }

        return View(movie);
    }
}

