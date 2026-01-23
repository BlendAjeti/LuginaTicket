using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LuginaTicket.Data;
using LuginaTicket.Models;
using LuginaTicket.Services;
using System.Security.Claims;
using System.IO;

namespace LuginaTicket.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class MoviesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IActionLogService _actionLogService;

    public MoviesController(ApplicationDbContext context, IActionLogService actionLogService)
    {
        _context = context;
        _actionLogService = actionLogService;
    }

    // GET: Admin/Movies
    public async Task<IActionResult> Index(string? search, string? genre, int page = 1, int pageSize = 10)
    {
        var query = _context.Movies.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(m => m.Title.Contains(search) || m.Description.Contains(search));
        }

        if (!string.IsNullOrEmpty(genre))
        {
            query = query.Where(m => m.Genre.Contains(genre));
        }

        var totalCount = await query.CountAsync();
        var movies = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.Genre = genre;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
        {
            await _actionLogService.LogActionAsync(userId, "Read", "Movie", null, "Viewed movies list");
        }

        return View(movies);
    }

    // GET: Admin/Movies/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var movie = await _context.Movies
            .Include(m => m.Showtimes)
                .ThenInclude(s => s.CinemaHall)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (movie == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
        {
            await _actionLogService.LogActionAsync(userId, "Read", "Movie", id, $"Viewed movie details: {movie.Title}");
        }

        return View(movie);
    }

    // GET: Admin/Movies/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Admin/Movies/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Movie movie, IFormFile? posterFile)
    {
        if (ModelState.IsValid)
        {
            // Handle image upload
            if (posterFile != null && posterFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "movies");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(posterFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await posterFile.CopyToAsync(fileStream);
                }

                movie.PosterUrl = $"/images/movies/{uniqueFileName}";
            }

            movie.CreatedAt = DateTime.UtcNow;
            _context.Add(movie);
            await _context.SaveChangesAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                await _actionLogService.LogActionAsync(userId, "Create", "Movie", movie.Id, $"Created movie: {movie.Title}");
            }

            return RedirectToAction(nameof(Index));
        }
        return View(movie);
    }

    // GET: Admin/Movies/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var movie = await _context.Movies.FindAsync(id);
        if (movie == null)
        {
            return NotFound();
        }
        return View(movie);
    }

    // POST: Admin/Movies/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Movie movie)
    {
        if (id != movie.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                movie.UpdatedAt = DateTime.UtcNow;
                _context.Update(movie);
                await _context.SaveChangesAsync();

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId != null)
                {
                    await _actionLogService.LogActionAsync(userId, "Update", "Movie", id, $"Updated movie: {movie.Title}");
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MovieExists(movie.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(movie);
    }

    // GET: Admin/Movies/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var movie = await _context.Movies
            .FirstOrDefaultAsync(m => m.Id == id);
        if (movie == null)
        {
            return NotFound();
        }

        return View(movie);
    }

    // POST: Admin/Movies/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var movie = await _context.Movies
            .Include(m => m.Showtimes)
                .ThenInclude(s => s.Seats)
                    .ThenInclude(seat => seat.Ticket)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (movie != null)
        {
            var title = movie.Title;

            // Collect all tickets to delete
            var ticketsToDelete = new List<Ticket>();
            foreach (var showtime in movie.Showtimes)
            {
                foreach (var seat in showtime.Seats)
                {
                    if (seat.Ticket != null)
                    {
                        ticketsToDelete.Add(seat.Ticket);
                    }
                }
            }

            // Delete all tickets
            if (ticketsToDelete.Any())
            {
                _context.Tickets.RemoveRange(ticketsToDelete);
            }

            // Delete all seats (they're already loaded via Include)
            var allSeats = movie.Showtimes.SelectMany(s => s.Seats).ToList();
            _context.Seats.RemoveRange(allSeats);

            // Delete all showtimes
            _context.Showtimes.RemoveRange(movie.Showtimes);

            // Delete the movie
            _context.Movies.Remove(movie);

            await _context.SaveChangesAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                await _actionLogService.LogActionAsync(userId, "Delete", "Movie", id, $"Deleted movie: {title}");
            }
        }

        return RedirectToAction(nameof(Index));
    }

    private bool MovieExists(int id)
    {
        return _context.Movies.Any(e => e.Id == id);
    }
}

