using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LuginaTicket.Data;
using LuginaTicket.Models;
using LuginaTicket.Services;
using System.Security.Claims;

namespace LuginaTicket.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ShowtimesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IActionLogService _actionLogService;

    public ShowtimesController(ApplicationDbContext context, IActionLogService actionLogService)
    {
        _context = context;
        _actionLogService = actionLogService;
    }

    // GET: Admin/Showtimes/Create?movieId=5
    public async Task<IActionResult> Create(int? movieId)
    {
        if (movieId == null)
        {
            return NotFound();
        }

        var movie = await _context.Movies.FindAsync(movieId);
        if (movie == null)
        {
            return NotFound();
        }

        var showtime = new Showtime
        {
            MovieId = movieId.Value,
            ShowDateTime = DateTime.Now.AddDays(1).Date.AddHours(18) // Default to tomorrow at 6 PM
        };

        ViewBag.MovieId = movieId;
        ViewBag.MovieTitle = movie.Title;
        
        var cinemaHalls = await _context.CinemaHalls
            .Where(h => h.IsActive)
            .OrderBy(h => h.Name)
            .ToListAsync();
        
        ViewBag.CinemaHalls = new SelectList(cinemaHalls, "Id", "Name");

        return View(showtime);
    }

    // POST: Admin/Showtimes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("MovieId,CinemaHallId,ShowDateTime,ViewType,Price,IsActive")] Showtime showtime)
    {
        // Remove validation errors for navigation properties since we only bind foreign keys
        ModelState.Remove("Movie");
        ModelState.Remove("CinemaHall");
        ModelState.Remove("Seats");
        ModelState.Remove("Tickets");

        if (ModelState.IsValid)
        {
            showtime.CreatedAt = DateTime.UtcNow;
            _context.Add(showtime);
            await _context.SaveChangesAsync();

            // Create seats for the showtime
            var cinemaHall = await _context.CinemaHalls.FindAsync(showtime.CinemaHallId);
            if (cinemaHall != null)
            {
                var seats = new List<Seat>();
                var rows = new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P" };
                
                for (int rowIndex = 0; rowIndex < Math.Min(cinemaHall.TotalRows, rows.Length); rowIndex++)
                {
                    for (int seatNum = 1; seatNum <= cinemaHall.SeatsPerRow; seatNum++)
                    {
                        seats.Add(new Seat
                        {
                            ShowtimeId = showtime.Id,
                            CinemaHallId = cinemaHall.Id,
                            Row = rows[rowIndex],
                            Number = seatNum,
                            Status = SeatStatus.Available,
                            IsWheelchairAccessible = rowIndex == 0 && seatNum == 1,
                            IsVIP = false
                        });
                    }
                }

                _context.Seats.AddRange(seats);
                await _context.SaveChangesAsync();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                await _actionLogService.LogActionAsync(userId, "Create", "Showtime", showtime.Id, $"Created showtime for movie ID: {showtime.MovieId}");
            }

            return RedirectToAction("Details", "Movies", new { id = showtime.MovieId });
        }

        ViewBag.MovieId = showtime.MovieId;
        var movie = await _context.Movies.FindAsync(showtime.MovieId);
        ViewBag.MovieTitle = movie?.Title;
        
        var cinemaHalls = await _context.CinemaHalls
            .Where(h => h.IsActive)
            .OrderBy(h => h.Name)
            .ToListAsync();
        
        ViewBag.CinemaHalls = new SelectList(cinemaHalls, "Id", "Name");

        return View(showtime);
    }

    // GET: Admin/Showtimes/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var showtime = await _context.Showtimes.FindAsync(id);
        if (showtime == null)
        {
            return NotFound();
        }

        var cinemaHalls = await _context.CinemaHalls
            .Where(h => h.IsActive)
            .OrderBy(h => h.Name)
            .ToListAsync();
        
        ViewBag.CinemaHalls = new SelectList(cinemaHalls, "Id", "Name", showtime.CinemaHallId);

        return View(showtime);
    }

    // POST: Admin/Showtimes/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,MovieId,CinemaHallId,ShowDateTime,ViewType,Price,IsActive,CreatedAt")] Showtime showtime)
    {
        if (id != showtime.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(showtime);
                await _context.SaveChangesAsync();

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId != null)
                {
                    await _actionLogService.LogActionAsync(userId, "Update", "Showtime", id, $"Updated showtime ID: {id}");
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ShowtimeExists(showtime.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction("Details", "Movies", new { id = showtime.MovieId });
        }

        var cinemaHalls = await _context.CinemaHalls
            .Where(h => h.IsActive)
            .OrderBy(h => h.Name)
            .ToListAsync();
        
        ViewBag.CinemaHalls = new SelectList(cinemaHalls, "Id", "Name", showtime.CinemaHallId);

        return View(showtime);
    }

    // GET: Admin/Showtimes/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var showtime = await _context.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.CinemaHall)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (showtime == null)
        {
            return NotFound();
        }

        return View(showtime);
    }

    // POST: Admin/Showtimes/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var showtime = await _context.Showtimes.FindAsync(id);
        if (showtime != null)
        {
            var movieId = showtime.MovieId;
            _context.Showtimes.Remove(showtime);
            await _context.SaveChangesAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                await _actionLogService.LogActionAsync(userId, "Delete", "Showtime", id, $"Deleted showtime ID: {id}");
            }

            return RedirectToAction("Details", "Movies", new { id = movieId });
        }

        return NotFound();
    }

    private bool ShowtimeExists(int id)
    {
        return _context.Showtimes.Any(e => e.Id == id);
    }
}

