using System.ComponentModel.DataAnnotations;

namespace LuginaTicket.ViewModels;

public class UserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class CreateUserViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "User";
}

public class EditUserViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    [Required]
    public string Role { get; set; } = "User";
}
public class DashboardViewModel
{
    public int TotalUsers { get; set; }
    public int TotalMovies { get; set; }
    public int TotalTickets { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<LuginaTicket.Models.Ticket> RecentTickets { get; set; } = new();
    public List<GenreCount> MoviesByGenre { get; set; } = new();
}

public class GenreCount
{
    public string Genre { get; set; } = string.Empty;
    public int Count { get; set; }
}


