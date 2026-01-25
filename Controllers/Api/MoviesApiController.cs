using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LuginaTicket.Data;
using LuginaTicket.Models;
using LuginaTicket.Services;
using System.Security.Claims;

namespace LuginaTicket.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class MoviesApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IActionLogService _actionLogService;

    public MoviesApiController(ApplicationDbContext context, IActionLogService actionLogService)
    {
        _context = context;
        _actionLogService = actionLogService;
    }

    // GET: api/MoviesApi
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Movie>>> GetMovies()
    {
        var movies = await _context.Movies
            .Where(m => m.IsActive)
            .ToListAsync();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
        {
            await _actionLogService.LogActionAsync(userId, "Read", "Movie", null, "API: Get movies");
        }

        return Ok(movies);
    }

    // GET: api/MoviesApi/5
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Movie>> GetMovie(int id)
    {
        var movie = await _context.Movies.FindAsync(id);

        if (movie == null || !movie.IsActive)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
        {
            await _actionLogService.LogActionAsync(userId, "Read", "Movie", id, $"API: Get movie {id}");
        }

        return Ok(movie);
    }

    // POST: api/MoviesApi
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Movie>> PostMovie(Movie movie)
    {
        movie.CreatedAt = DateTime.UtcNow;
        _context.Movies.Add(movie);
        await _context.SaveChangesAsync();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
        {
            await _actionLogService.LogActionAsync(userId, "Create", "Movie", movie.Id, $"API: Created movie {movie.Title}");
        }

        return CreatedAtAction("GetMovie", new { id = movie.Id }, movie);
    }

    // PUT: api/MoviesApi/5
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PutMovie(int id, Movie movie)
    {
        if (id != movie.Id)
        {
            return BadRequest();
        }

        movie.UpdatedAt = DateTime.UtcNow;
        _context.Entry(movie).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                await _actionLogService.LogActionAsync(userId, "Update", "Movie", id, $"API: Updated movie {movie.Title}");
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!MovieExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // DELETE: api/MoviesApi/5
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteMovie(int id)
    {
        var movie = await _context.Movies.FindAsync(id);
        if (movie == null)
        {
            return NotFound();
        }

        _context.Movies.Remove(movie);
        await _context.SaveChangesAsync();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
        {
            await _actionLogService.LogActionAsync(userId, "Delete", "Movie", id, $"API: Deleted movie {movie.Title}");
        }

        return NoContent();
    }

    private bool MovieExists(int id)
    {
        return _context.Movies.Any(e => e.Id == id);
    }
}

