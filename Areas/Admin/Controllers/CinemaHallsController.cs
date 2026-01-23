using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LuginaTicket.Data;
using LuginaTicket.Models;
using LuginaTicket.Services;
using System.Security.Claims;

namespace LuginaTicket.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CinemaHallsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IActionLogService _actionLogService;

    public CinemaHallsController(ApplicationDbContext context, IActionLogService actionLogService)
    {
        _context = context;
        _actionLogService = actionLogService;
    }

    // GET: Admin/CinemaHalls
    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
    {
        var query = _context.CinemaHalls.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(h => h.Name.Contains(search) || h.Location.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var cinemaHalls = await query
            .OrderBy(h => h.Location)
            .ThenBy(h => h.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
        {
            await _actionLogService.LogActionAsync(userId, "Read", "CinemaHall", null, "Viewed cinema halls list");
        }

        return View(cinemaHalls);
    }

    // GET: Admin/CinemaHalls/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var cinemaHall = await _context.CinemaHalls
            .Include(h => h.Showtimes)
                .ThenInclude(s => s.Movie)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (cinemaHall == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
        {
            await _actionLogService.LogActionAsync(userId, "Read", "CinemaHall", id, $"Viewed cinema hall details: {cinemaHall.Name}");
        }

        return View(cinemaHall);
    }

    // GET: Admin/CinemaHalls/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Admin/CinemaHalls/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CinemaHall cinemaHall)
    {
        if (ModelState.IsValid)
        {
            _context.Add(cinemaHall);
            await _context.SaveChangesAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                await _actionLogService.LogActionAsync(userId, "Create", "CinemaHall", cinemaHall.Id, $"Created cinema hall: {cinemaHall.Name}");
            }

            return RedirectToAction(nameof(Index));
        }
        return View(cinemaHall);
    }

    // GET: Admin/CinemaHalls/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var cinemaHall = await _context.CinemaHalls.FindAsync(id);
        if (cinemaHall == null)
        {
            return NotFound();
        }
        return View(cinemaHall);
    }

    // POST: Admin/CinemaHalls/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CinemaHall cinemaHall)
    {
        if (id != cinemaHall.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(cinemaHall);
                await _context.SaveChangesAsync();

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId != null)
                {
                    await _actionLogService.LogActionAsync(userId, "Update", "CinemaHall", id, $"Updated cinema hall: {cinemaHall.Name}");
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CinemaHallExists(cinemaHall.Id))
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
        return View(cinemaHall);
    }

    // GET: Admin/CinemaHalls/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var cinemaHall = await _context.CinemaHalls
            .FirstOrDefaultAsync(m => m.Id == id);
        if (cinemaHall == null)
        {
            return NotFound();
        }

        return View(cinemaHall);
    }

    // POST: Admin/CinemaHalls/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var cinemaHall = await _context.CinemaHalls.FindAsync(id);
        if (cinemaHall != null)
        {
            var name = cinemaHall.Name;
            _context.CinemaHalls.Remove(cinemaHall);
            await _context.SaveChangesAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                await _actionLogService.LogActionAsync(userId, "Delete", "CinemaHall", id, $"Deleted cinema hall: {name}");
            }
        }

        return RedirectToAction(nameof(Index));
    }

    private bool CinemaHallExists(int id)
    {
        return _context.CinemaHalls.Any(e => e.Id == id);
    }
}

