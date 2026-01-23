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
public class TicketsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IActionLogService _actionLogService;

    public TicketsController(ApplicationDbContext context, IActionLogService actionLogService)
    {
        _context = context;
        _actionLogService = actionLogService;
    }

    // GET: Admin/Tickets
    public async Task<IActionResult> Index(string? search, string? status, int page = 1, int pageSize = 20)
    {
        var query = _context.Tickets
            .Include(t => t.User)
            .Include(t => t.Showtime)
                .ThenInclude(s => s.Movie)
            .Include(t => t.Seat)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(t => t.TicketNumber.Contains(search) ||
                                    t.User!.UserName!.Contains(search) ||
                                    t.Showtime.Movie.Title.Contains(search));
        }

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<TicketStatus>(status, out var statusEnum))
        {
            query = query.Where(t => t.Status == statusEnum);
        }

        var totalCount = await query.CountAsync();
        var tickets = await query
            .OrderByDescending(t => t.PurchaseDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        ViewBag.Statuses = Enum.GetValues(typeof(TicketStatus)).Cast<TicketStatus>().ToList();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
        {
            await _actionLogService.LogActionAsync(userId, "Read", "Ticket", null, "Viewed tickets list");
        }

        return View(tickets);
    }

    // GET: Admin/Tickets/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var ticket = await _context.Tickets
            .Include(t => t.User)
            .Include(t => t.Showtime)
                .ThenInclude(s => s.Movie)
            .Include(t => t.Showtime)
                .ThenInclude(s => s.CinemaHall)
            .Include(t => t.Seat)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (ticket == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
        {
            await _actionLogService.LogActionAsync(userId, "Read", "Ticket", id, $"Viewed ticket details: {ticket.TicketNumber}");
        }

        return View(ticket);
    }

    // GET: Admin/Tickets/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var ticket = await _context.Tickets
            .Include(t => t.User)
            .Include(t => t.Showtime)
                .ThenInclude(s => s.Movie)
            .Include(t => t.Seat)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
        {
            return NotFound();
        }

        ViewBag.Statuses = Enum.GetValues(typeof(TicketStatus)).Cast<TicketStatus>().ToList();
        return View(ticket);
    }

    // POST: Admin/Tickets/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Ticket ticket)
    {
        if (id != ticket.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(ticket);
                await _context.SaveChangesAsync();

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId != null)
                {
                    await _actionLogService.LogActionAsync(userId, "Update", "Ticket", id, $"Updated ticket: {ticket.TicketNumber}");
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TicketExists(ticket.Id))
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

        ViewBag.Statuses = Enum.GetValues(typeof(TicketStatus)).Cast<TicketStatus>().ToList();
        return View(ticket);
    }

    // GET: Admin/Tickets/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var ticket = await _context.Tickets
            .Include(t => t.User)
            .Include(t => t.Showtime)
                .ThenInclude(s => s.Movie)
            .Include(t => t.Seat)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (ticket == null)
        {
            return NotFound();
        }

        return View(ticket);
    }

    // POST: Admin/Tickets/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket != null)
        {
            var ticketNumber = ticket.TicketNumber;
            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                await _actionLogService.LogActionAsync(userId, "Delete", "Ticket", id, $"Deleted ticket: {ticketNumber}");
            }
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: Admin/Tickets/ViewTicket/5
    public async Task<IActionResult> ViewTicket(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var ticket = await _context.Tickets
            .Include(t => t.User)
            .Include(t => t.Showtime)
                .ThenInclude(s => s.Movie)
            .Include(t => t.Showtime)
                .ThenInclude(s => s.CinemaHall)
            .Include(t => t.Seat)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (ticket == null)
        {
            return NotFound();
        }

        return View("TicketView", ticket);
    }

    private bool TicketExists(int id)
    {
        return _context.Tickets.Any(e => e.Id == id);
    }
}

