using LuginaTicket.Data;
using LuginaTicket.Models;
using Microsoft.AspNetCore.Http;

namespace LuginaTicket.Services;

public class ActionLogService : IActionLogService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ActionLogService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogActionAsync(string userId, string action, string entityType, int? entityId = null, string? details = null)
    {
        var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        
        var log = new ActionLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            IpAddress = ipAddress
        };

        _context.ActionLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}

