namespace LuginaTicket.Services;

public interface IActionLogService
{
    Task LogActionAsync(string userId, string action, string entityType, int? entityId = null, string? details = null);
}

